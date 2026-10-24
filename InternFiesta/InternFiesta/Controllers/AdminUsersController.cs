
using System.Data;
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Administrator")]
    [Route("Admin/Users")]
    public class AdminUsersController : Controller
    {
        private const int PageSize = 20;

        private static readonly string[] ManagedRoles =
        {
            "Student",
            "Company",
            "Alumni",
            "FacultySupervisor",
            "UniversityCoordinator",
            "Administrator"
        };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminUsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ADMIN: LIST, SEARCH, FILTER USERS
        [HttpGet("")]
        public async Task<IActionResult> Index(
        string? q, string? role, string? status, int page = 1)
        {
            string search = q?.Trim() ?? string.Empty;

            if (search.Length > 100)
            {
                return BadRequest(
                    "Search must be 100 characters or fewer.");
            }

            var availableRoles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.Name != null)
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            string selectedRole = string.Empty;

            if (!string.IsNullOrWhiteSpace(role))
            {
                selectedRole = availableRoles.FirstOrDefault(r =>
                    string.Equals(
                        r, role.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    ?? string.Empty;

                if (selectedRole.Length == 0)
                    return BadRequest("Invalid role filter.");
            }


            string selectedStatus =
                status?.Trim().ToLowerInvariant() ?? string.Empty;

            if (selectedStatus != string.Empty &&
                selectedStatus != "active" &&
                selectedStatus != "disabled")
            {
                return BadRequest("Invalid account status filter.");
            }

            IQueryable<ApplicationUser> users =
                _userManager.Users.AsNoTracking();

            if (selectedStatus == "active")
            {
                users = users.Where(u => !u.IsDisabled);
            }
            else if (selectedStatus == "disabled")
            {
                users = users.Where(u => u.IsDisabled);
            }


            if (search.Length > 0)
            {
                users = users.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null &&
                     u.Email.Contains(search)));
            }

            if (selectedRole.Length > 0)
            {
                var matchingUserIds =
                    from membership in _context.UserRoles
                    join roleRecord in _context.Roles
                        on membership.RoleId equals roleRecord.Id
                    where roleRecord.Name == selectedRole
                    select membership.UserId;

                users = users.Where(u =>
                    matchingUserIds.Contains(u.Id));
            }

            int totalUsers = await users.CountAsync();

            int totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    totalUsers / (double)PageSize));

            page = Math.Clamp(page, 1, totalPages);

            var pageUsers = await users
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(u => new AdminUserRowViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    EmailConfirmed = u.EmailConfirmed,
                    IsDisabled = u.IsDisabled
                })
                .ToListAsync();

            var ids = pageUsers
                .Select(u => u.Id)
                .ToList();

            var memberships = await (
                from membership in
                    _context.UserRoles.AsNoTracking()

                join roleRecord in
                    _context.Roles.AsNoTracking()

                    on membership.RoleId equals roleRecord.Id

                where ids.Contains(membership.UserId)

                select new
                {
                    membership.UserId,
                    roleRecord.Name
                }
            ).ToListAsync();

            var rolesByUser = memberships
                .GroupBy(m => m.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(m => m.Name ?? string.Empty)
                        .Where(n => n.Length > 0)
                        .OrderBy(n => n)
                        .ToList());

            foreach (var user in pageUsers)
            {
                if (rolesByUser.TryGetValue(
                    user.Id, out var roles))
                {
                    user.Roles = roles;
                }
            }


            return View(new AdminUsersIndexViewModel
            {
                Search = search,
                SelectedRole = selectedRole,
                SelectedStatus = selectedStatus,
                AvailableRoles = availableRoles,
                Users = pageUsers,
                TotalUsers = totalUsers,
                Page = page,
                TotalPages = totalPages
            });

        }

        // ADMIN: VIEW ACCOUNT DETAILS
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            bool isSelf =
                _userManager.GetUserId(User) == user.Id;

            bool isLastActiveAdmin =
                !user.IsDisabled &&
                roles.Contains("Administrator") &&
                await CountActiveAdminsAsync() <= 1;

            return View(new AdminUserDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,

                Roles = roles.OrderBy(r => r).ToList(),
                IsDisabled = user.IsDisabled,
                IsCurrentUser = isSelf,

                IsLastActiveAdministrator =
                    isLastActiveAdmin,

                AvailableRoles = ManagedRoles.ToList()
            });
        }

        // ADMIN: CHANGE ROLE
        [HttpPost("ChangeRole/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            string id,
            string newRole)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            if (!ManagedRoles.Contains(
                newRole, StringComparer.Ordinal))
            {
                return BadRequest("Unsupported role.");
            }

            var actorId = _userManager.GetUserId(User);

            if (actorId == null)
                return Challenge();

            if (actorId == id)
            {
                return ShowError(
                    id,
                    "You cannot change your own role.");
            }

            // Protect against simultaneous admin changes.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // Recheck the acting Administrator.
            var actor =
                await _userManager.FindByIdAsync(actorId);

            if (actor == null ||
                actor.IsDisabled ||
                !await _userManager.IsInRoleAsync(
                    actor, "Administrator"))
            {
                return Forbid();
            }

            var target =
                await _userManager.FindByIdAsync(id);

            if (target == null)
                return NotFound();

            if (target.IsDisabled)
            {
                return ShowError(
                    id,
                    "Restore the account before changing its role.");
            }

            var currentRoles =
                await _userManager.GetRolesAsync(target);

            // InternFiesta currently uses one primary role.
            if (currentRoles.Count != 1)
            {
                return ShowError(
                    id,
                    "This account has zero or multiple roles. " +
                    "Review it manually first.");
            }

            string oldRole = currentRoles[0];

            if (oldRole == newRole)
            {
                TempData["WarningMessage"] =
                    "This role is already assigned.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            // Privileged academic/admin accounts
            // must have a verified email.
            if ((newRole == "Administrator" ||
                 newRole == "FacultySupervisor" ||
                 newRole == "UniversityCoordinator") &&
                !target.EmailConfirmed)
            {
                return ShowError(
                    id,
                    "Verify the account email before " +
                    "assigning a privileged role.");
            }

            // Prevent removing the last active Admin.
            if (oldRole == "Administrator" &&
                await CountActiveAdminsAsync() <= 1)
            {
                return ShowError(
                    id,
                    "The last active Administrator " +
                    "cannot be demoted.");
            }

            // Prevent role changes that leave
            // existing records without a suitable owner.
            if (await HasBusinessActivityAsync(id))
            {
                return ShowError(
                    id,
                    "Role change blocked: this account already " +
                    "owns academic, recruitment, job-fair, " +
                    "or mentoring records. Resolve the records " +
                    "through an approved transfer process.");
            }

            var removeResult =
                await _userManager.RemoveFromRoleAsync(
                    target, oldRole);

            if (!removeResult.Succeeded)
            {
                return ShowError(
                    id, DescribeErrors(removeResult));
            }

            var addResult =
                await _userManager.AddToRoleAsync(
                    target, newRole);

            if (!addResult.Succeeded)
            {
                return ShowError(
                    id, DescribeErrors(addResult));
            }

            // Old Alumni profiles must not remain public
            // after the account changes to another role.
            if (oldRole == "Alumni" &&
                newRole != "Alumni")
            {
                var alumniProfile =
                    await _context.AlumniProfiles
                        .FirstOrDefaultAsync(
                            p => p.UserId == id);

                if (alumniProfile != null)
                {
                    alumniProfile.IsVisibleToStudents = false;
                    alumniProfile.IsAvailableForMentoring = false;

                    await _context.SaveChangesAsync();
                }
            }

            // Revoke existing authentication sessions.
            var stampResult =
                await _userManager.UpdateSecurityStampAsync(
                    target);

            if (!stampResult.Succeeded)
            {
                return ShowError(
                    id, DescribeErrors(stampResult));
            }

            await transaction.CommitAsync();

            _logger.LogWarning(
                "Admin {ActorId} changed role for user " +
                "{TargetId}: {OldRole} -> {NewRole}",
                actorId, id, oldRole, newRole);

            TempData["SuccessMessage"] =
                "Role changed. The user must sign in again.";

            return RedirectToAction(
                nameof(Details), new { id });
        }

        // ADMIN: DISABLE OR RESTORE ACCOUNT
        [HttpPost("SetStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(
            string id,
            string action)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            if (action != "Disable" &&
                action != "Restore")
            {
                return BadRequest("Invalid account action.");
            }

            var actorId = _userManager.GetUserId(User);

            if (actorId == null)
                return Challenge();

            if (actorId == id)
            {
                return ShowError(
                    id,
                    "You cannot disable or restore " +
                    "your own account.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var actor =
                await _userManager.FindByIdAsync(actorId);

            if (actor == null ||
                actor.IsDisabled ||
                !await _userManager.IsInRoleAsync(
                    actor, "Administrator"))
            {
                return Forbid();
            }

            var target =
                await _userManager.FindByIdAsync(id);

            if (target == null)
                return NotFound();

            bool disable = action == "Disable";

            if (target.IsDisabled == disable)
            {
                TempData["WarningMessage"] = disable
                    ? "Account is already disabled."
                    : "Account is already active.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            if (disable &&
                await _userManager.IsInRoleAsync(
                    target, "Administrator") &&
                await CountActiveAdminsAsync() <= 1)
            {
                return ShowError(
                    id,
                    "The last active Administrator " +
                    "cannot be disabled.");
            }

            target.IsDisabled = disable;

            var saveResult =
                await _userManager.UpdateAsync(target);

            if (!saveResult.Succeeded)
            {
                return ShowError(
                    id, DescribeErrors(saveResult));
            }

            // Existing sessions are invalidated.
            var stampResult =
                await _userManager.UpdateSecurityStampAsync(
                    target);

            if (!stampResult.Succeeded)
            {
                return ShowError(
                    id, DescribeErrors(stampResult));
            }

            await transaction.CommitAsync();

            _logger.LogWarning(
                "Admin {ActorId} {Action} account {TargetId}",
                actorId, action, id);

            TempData["SuccessMessage"] = disable
                ? "Account disabled. Existing sessions are revoked."
                : "Account restored. The user can sign in again.";

            return RedirectToAction(
                nameof(Details), new { id });
        }

        // Count only currently enabled Administrators.
        private async Task<int> CountActiveAdminsAsync()
        {
            return await (
                from user in _context.Users.AsNoTracking()

                join membership in
                    _context.UserRoles.AsNoTracking()

                    on user.Id equals membership.UserId

                join roleRecord in
                    _context.Roles.AsNoTracking()

                    on membership.RoleId equals roleRecord.Id

                where roleRecord.Name == "Administrator"
                      && !user.IsDisabled

                select user.Id
            ).Distinct().CountAsync();
        }

        // A role change must not strand business records.
        private async Task<bool> HasBusinessActivityAsync(
            string id)
        {
            return
                await _context.InternshipApplications
                    .AnyAsync(a => a.StudentUserId == id)

                || await _context.SavedInternships
                    .AnyAsync(s => s.StudentUserId == id)

                || await _context.InternshipPostings
                    .IgnoreQueryFilters()
                    .AnyAsync(p => p.CompanyUserId == id)

                || await _context.JobFairRegistrations
                    .AnyAsync(r => r.UserId == id)

                || await _context.JobFairBooths
                    .AnyAsync(b => b.CompanyUserId == id)

                || await _context.JobFairs
                    .AnyAsync(f =>
                        f.CreatedByCoordinatorId == id)

                || await _context.AcademicInternships
                    .AnyAsync(a => a.FacultyUserId == id)

                || await _context.FacultyEvaluations
                    .AnyAsync(e => e.FacultyUserId == id)

                || await _context.AcademicCompanyFeedbacks
                    .AnyAsync(f => f.CompanyUserId == id)

                || await _context.AlumniMentoringRequests
                    .AnyAsync(r => r.StudentUserId == id)

                || await _context.AlumniMentoringRequests
                    .AnyAsync(r =>
                        r.AlumniProfile.UserId == id);
        }

        private IActionResult ShowError(
            string id,
            string message)
        {
            TempData["ErrorMessage"] = message;

            return RedirectToAction(
                nameof(Details), new { id });
        }

        private static string DescribeErrors(
            IdentityResult result)
        {
            return string.Join(
                "; ",
                result.Errors.Select(e => e.Description));
        }
    }
}
