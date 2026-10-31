
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Student"))
                return View("Student");

            if (User.IsInRole("Company"))
                return await CompanyDashboard();

            if (User.IsInRole("FacultySupervisor"))
                return View("Faculty");

            if (User.IsInRole("UniversityCoordinator"))
                return RedirectToAction("Index", "CoordinatorOverview");

            if (User.IsInRole("Alumni"))
                return View("Alumni");

            if (User.IsInRole("Administrator"))
                return View("Administrator");

            return Forbid();
        }

        private async Task<IActionResult> CompanyDashboard()
        {
            var companyId = _userManager.GetUserId(User);

            if (companyId == null)
                return Challenge();

            var postings = await _context.InternshipPostings
                .AsNoTracking()
                .Where(p => p.CompanyUserId == companyId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Deadline,
                    p.IsActive
                })
                .ToListAsync();

            var postingIds = postings
                .Select(p => p.Id)
                .ToList();

            var applications = await _context.InternshipApplications
                .AsNoTracking()
                .Where(a => postingIds.Contains(a.InternshipPostingId))
                .Select(a => new
                {
                    a.Id,
                    a.StudentUserId,
                    a.InternshipPostingId,
                    a.Status,
                    a.AppliedAt
                })
                .ToListAsync();

            var applicationIds = applications
                .Select(a => a.Id)
                .ToList();

            var scheduledIds = await _context.InterviewSchedules
                .AsNoTracking()
                .Where(i => applicationIds.Contains(i.InternshipApplicationId))
                .Select(i => i.InternshipApplicationId)
                .Distinct()
                .ToListAsync();

            var scheduledSet = scheduledIds.ToHashSet();

            var shortlisted = applications
                .Where(a => a.Status == "Shortlisted")
                .ToList();

            var pending = shortlisted
                .Where(a => !scheduledSet.Contains(a.Id))
                .OrderBy(a => a.AppliedAt)
                .ToList();

            var studentIds = pending
                .Select(a => a.StudentUserId)
                .Distinct()
                .ToList();

            var students = await _context.Users
                .AsNoTracking()
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .ToDictionaryAsync(u => u.Id);

            var postingTitles = postings
                .ToDictionary(p => p.Id, p => p.Title);

            var pendingInterviews = pending
                .Select(a =>
                {
                    students.TryGetValue(a.StudentUserId, out var student);
                    postingTitles.TryGetValue(a.InternshipPostingId, out var title);

                    return new CompanyInterviewCandidateViewModel
                    {
                        ApplicationId = a.Id,
                        StudentName = student?.FullName
                            ?? student?.Email
                            ?? "Student",
                        InternshipTitle = title ?? "Internship",
                        AppliedAt = a.AppliedAt
                    };
                })
                .ToList();

            var recentPostings = postings
                .Take(5)
                .Select(p => new CompanyRecentPostingViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Deadline = p.Deadline,
                    IsActive = p.IsActive,
                    ApplicationCount = applications.Count(a =>
                        a.InternshipPostingId == p.Id)
                })
                .ToList();

            var model = new CompanyDashboardViewModel
            {
                TotalPostings = postings.Count,
                TotalApplications = applications.Count,

                AppliedCount = applications.Count(a =>
                    a.Status == "Applied"),

                ShortlistedCount = shortlisted.Count,

                SelectedCount = applications.Count(a =>
                    a.Status == "Selected"),

                RejectedCount = applications.Count(a =>
                    a.Status == "Rejected"),

                WaitlistedCount = applications.Count(a =>
                    a.Status == "Waitlisted"),

                SelectedStudents = applications.Count(a =>
                    a.Status == "Selected"),

                ScheduledInterviews = scheduledIds.Count,

                AwaitingInterviewCount = pendingInterviews.Count,

                RecentPostings = recentPostings,
                PendingInterviews = pendingInterviews
            };

            return View("Company", model);
        }
    }
}
