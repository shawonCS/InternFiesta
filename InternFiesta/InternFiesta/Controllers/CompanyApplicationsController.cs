using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.Services;
using InternFiesta.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly
            UserManager<ApplicationUser> _userManager;

        public CompanyApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var companyUserId =
                _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var internship =
                await _context.InternshipPostings
                    .FirstOrDefaultAsync(i =>
                        i.Id == id &&
                        i.CompanyUserId == companyUserId);

            if (internship == null)
            {
                return NotFound();
            }

            var applications =
                await _context.InternshipApplications
                    .Where(a =>
                        a.InternshipPostingId == id)
                    .Include(a => a.Student)
                    .OrderByDescending(a =>
                        a.AppliedAt)
                    .ToListAsync();

            var studentIds =
                applications
                    .Select(a => a.StudentUserId)
                    .Distinct()
                    .ToList();

            var profiles =
                await _context.StudentProfiles
                    .Where(p =>
                        studentIds.Contains(p.UserId))
                    .ToDictionaryAsync(
                        p => p.UserId);

            var model =
                new CompanyApplicationsViewModel
                {
                    Internship = internship,

                    Applicants =
                        applications
                            .Select(a =>
                            {
                                var profile =
                                    profiles.GetValueOrDefault(
                                        a.StudentUserId);

                                var match =
                                    CandidateMatchService.Calculate(
                                        internship.RequiredSkills,
                                        profile?.Skills);

                                return new CompanyApplicantViewModel
                                {
                                    Application = a,
                                    Student = a.Student,
                                    Profile = profile,
                                    MatchScore = match.Score,
                                    MatchLabel = match.Label
                                };
                            })
                            .ToList()
                };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int applicationId,
            string status)
        {
            var companyUserId =
                _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var allowedStatuses =
                new[]
                {
            "Shortlisted",
            "Selected",
            "Rejected",
            "Waitlisted"
                };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest();
            }

            var application =
                await _context.InternshipApplications
                    .Include(a => a.InternshipPosting)
                    .FirstOrDefaultAsync(a =>
                        a.Id == applicationId &&
                        a.InternshipPosting != null &&
                        a.InternshipPosting.CompanyUserId ==
                            companyUserId);

            if (application == null)
            {
                return NotFound();
            }

            var oldStatus =
                application.Status;

            application.Status =
                status;

            // Create notification only if status actually changed
            if (oldStatus != status)
            {
                var notification =
                    new Notification
                    {
                        UserId =
                            application.StudentUserId,

                        Title =
                            "Application Status Updated",

                        Message =
                            $"Your application for " +
                            $"{application.InternshipPosting!.Title} " +
                            $"has been updated to {status}.",

                        IsRead =
                            false,

                        CreatedAt =
                            DateTime.Now
                    };

                _context.Notifications.Add(
                    notification);
            }

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] =
                $"Application status updated to {status}.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    id =
                        application.InternshipPostingId
                });
        }
    }
}