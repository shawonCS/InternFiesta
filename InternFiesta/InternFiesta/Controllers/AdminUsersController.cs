
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

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ADMIN: LIST AND SEARCH USERS
        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? q, string? role, int page = 1)
        {
            string search = q?.Trim() ?? string.Empty;

            if (search.Length > 100)
                return BadRequest(
                    "Search must be 100 characters or fewer.");

            // Load real Identity roles from the database.
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
                        r,
                        role.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    ?? string.Empty;

                if (selectedRole.Length == 0)
                    return BadRequest("Invalid role filter.");
            }

            IQueryable<ApplicationUser> users =
                _userManager.Users.AsNoTracking();

            // Filter users by name or email.
            if (search.Length > 0)
            {
                users = users.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null &&
                     u.Email.Contains(search)));
            }

            // Filter by actual Identity role membership.
            if (selectedRole.Length > 0)
            {
                var matchingUserIds =
                    from membership in _context.UserRoles
                    join identityRole in _context.Roles
                        on membership.RoleId equals identityRole.Id
                    where identityRole.Name == selectedRole
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

            // Load only one page at a time.
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
                    EmailConfirmed = u.EmailConfirmed
                })
                .ToListAsync();

            var pageUserIds = pageUsers
                .Select(u => u.Id)
                .ToList();

            // Load roles for the current page in one query.
            var pageMemberships = await (
                from membership in
                    _context.UserRoles.AsNoTracking()

                join identityRole in
                    _context.Roles.AsNoTracking()

                    on membership.RoleId equals identityRole.Id

                where pageUserIds.Contains(membership.UserId)

                select new
                {
                    membership.UserId,
                    RoleName = identityRole.Name
                }
            ).ToListAsync();

            var rolesByUser = pageMemberships
                .GroupBy(m => m.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(m =>
                            m.RoleName ?? string.Empty)
                        .Where(name => name.Length > 0)
                        .OrderBy(name => name)
                        .ToList());

            foreach (var user in pageUsers)
            {
                if (rolesByUser.TryGetValue(
                    user.Id, out var roles))
                {
                    user.Roles = roles;
                }
            }

            var model = new AdminUsersIndexViewModel
            {
                Search = search,
                SelectedRole = selectedRole,
                AvailableRoles = availableRoles,
                Users = pageUsers,
                TotalUsers = totalUsers,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
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

            var roleNames =
                await _userManager.GetRolesAsync(user);

            return View(new AdminUserDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roleNames
                    .OrderBy(name => name)
                    .ToList()
            });
        }
    }
}
