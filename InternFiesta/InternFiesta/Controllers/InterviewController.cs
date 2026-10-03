using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Company")]
    public class InterviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Schedule(
            int applicationId)
        {
            var companyUserId =
                _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var application =
                await _context.InternshipApplications
                    .Include(a => a.Student)
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

            if (application.Status != "Shortlisted")
            {
                TempData["InterviewError"] =
                    "Only shortlisted applicants can be scheduled for an interview.";

                return RedirectToAction(
                    "Index",
                    "CompanyApplications",
                    new
                    {
                        id = application.InternshipPostingId
                    });
            }

            var schedule =
                await _context.InterviewSchedules
                    .FirstOrDefaultAsync(i =>
                        i.InternshipApplicationId ==
                            applicationId);

            if (schedule == null)
            {
                var tomorrow =
                    DateTime.Now.AddDays(1);

                schedule = new InterviewSchedule
                {
                    InternshipApplicationId =
                        applicationId,

                    InterviewDateTime =
                        new DateTime(
                            tomorrow.Year,
                            tomorrow.Month,
                            tomorrow.Day,
                            tomorrow.Hour,
                            tomorrow.Minute,
                            0)
                };
            }

            ViewBag.StudentName =
                application.Student?.FullName
                ?? application.Student?.Email
                ?? "Student";

            ViewBag.InternshipTitle =
                application.InternshipPosting?.Title;

            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(
            InterviewSchedule model)
        {
            var companyUserId =
                _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var application =
                await _context.InternshipApplications
                    .Include(a => a.Student)
                    .Include(a => a.InternshipPosting)
                    .FirstOrDefaultAsync(a =>
                        a.Id ==
                            model.InternshipApplicationId &&
                        a.InternshipPosting != null &&
                        a.InternshipPosting.CompanyUserId ==
                            companyUserId);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Shortlisted")
            {
                return BadRequest();
            }

            if (model.InterviewDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.InterviewDateTime),
                    "Interview date and time must be in the future.");
            }

            if (string.IsNullOrWhiteSpace(
                model.LocationOrLink))
            {
                ModelState.AddModelError(
                    nameof(model.LocationOrLink),
                    "Location or meeting link is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.StudentName =
                    application.Student?.FullName
                    ?? application.Student?.Email
                    ?? "Student";

                ViewBag.InternshipTitle =
                    application.InternshipPosting?.Title;

                return View(model);
            }

            var existing =
                await _context.InterviewSchedules
                    .FirstOrDefaultAsync(i =>
                        i.InternshipApplicationId ==
                            model.InternshipApplicationId);

            var cleanInterviewTime =
                new DateTime(
                    model.InterviewDateTime.Year,
                    model.InterviewDateTime.Month,
                    model.InterviewDateTime.Day,
                    model.InterviewDateTime.Hour,
                    model.InterviewDateTime.Minute,
                    0);

            if (existing == null)
            {
                var schedule =
                    new InterviewSchedule
                    {
                        InternshipApplicationId =
                            model.InternshipApplicationId,

                        InterviewDateTime =
                            cleanInterviewTime,

                        LocationOrLink =
                            model.LocationOrLink.Trim(),

                        Notes =
                            model.Notes?.Trim(),

                        CreatedAt =
                            DateTime.Now
                    };

                _context.InterviewSchedules.Add(
                    schedule);
            }
            else
            {
                existing.InterviewDateTime =
                    cleanInterviewTime;

                existing.LocationOrLink =
                    model.LocationOrLink.Trim();

                existing.Notes =
                    model.Notes?.Trim();

                existing.UpdatedAt =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["InterviewMessage"] =
                existing == null
                    ? "Interview scheduled successfully."
                    : "Interview rescheduled successfully.";

            return RedirectToAction(
                "Index",
                "CompanyApplications",
                new
                {
                    id = application.InternshipPostingId
                });
        }
    }
}