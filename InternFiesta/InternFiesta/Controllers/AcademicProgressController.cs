
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Controllers
{
    [Authorize]
    public class AcademicProgressController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicProgressController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class ProgressForm
        {
            [Required]
            [StringLength(2000)]
            public string Description { get; set; } = "";
        }

        public class FeedbackForm
        {
            [Range(1, 5)]
            public int Rating { get; set; }

            [Required]
            [StringLength(2000)]
            public string Comments { get; set; } = "";
        }

        public class AssignmentForm
        {
            [Range(1, int.MaxValue)]
            public int ApplicationId { get; set; }

            [Required]
            public string FacultyUserId { get; set; } = "";
        }

        // COORDINATOR: Selected applications
        [HttpGet]
        [Authorize(Roles = "UniversityCoordinator")]
        public async Task<IActionResult> AssignFaculty()
        {
            var applications = await _context.InternshipApplications
                .AsNoTracking()
                .Where(a => a.Status == "Selected")
                .Include(a => a.Student)
                .Include(a => a.InternshipPosting)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var faculty = await _userManager
                .GetUsersInRoleAsync("FacultySupervisor");

            var assignments = await _context.AcademicInternships
                .AsNoTracking()
                .ToDictionaryAsync(
                    a => a.InternshipApplicationId,
                    a => a.FacultyUserId);

            ViewBag.Faculty = faculty;
            ViewBag.Assignments = assignments;

            return View(applications);
        }

        // COORDINATOR: Create/update assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "UniversityCoordinator")]
        public async Task<IActionResult> AssignFaculty(
            AssignmentForm model)
        {
            if (!ModelState.IsValid)
            {
                TempData["StatusMessage"] =
                    "Please select a valid application and faculty.";
                return RedirectToAction(nameof(AssignFaculty));
            }

            var application = await _context.InternshipApplications
                .FirstOrDefaultAsync(a =>
                    a.Id == model.ApplicationId &&
                    a.Status == "Selected");

            if (application == null)
                return NotFound();

            var faculty = await _userManager
                .FindByIdAsync(model.FacultyUserId);

            if (faculty == null ||
                !await _userManager.IsInRoleAsync(
                    faculty, "FacultySupervisor"))
            {
                TempData["StatusMessage"] =
                    "Invalid faculty supervisor.";
                return RedirectToAction(nameof(AssignFaculty));
            }

            var academic = await _context.AcademicInternships
                .FirstOrDefaultAsync(a =>
                    a.InternshipApplicationId == application.Id);

            if (academic == null)
            {
                _context.AcademicInternships.Add(
                    new AcademicInternship
                    {
                        InternshipApplicationId = application.Id,
                        FacultyUserId = faculty.Id,
                        StartedAt = DateTime.UtcNow
                    });
            }
            else
            {
                academic.FacultyUserId = faculty.Id;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Faculty assigned successfully.";

            return RedirectToAction(nameof(AssignFaculty));
        }

        // STUDENT: Open form using Application ID
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitProgress(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var academic = await _context.AcademicInternships
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.InternshipApplicationId == id &&
                    a.InternshipApplication.StudentUserId == userId &&
                    a.InternshipApplication.Status == "Selected");

            if (academic == null)
            {
                TempData["StatusMessage"] =
                    "Faculty assignment is required before submitting progress.";

                return RedirectToAction(
                    "MyApplications", "StudentInternships");
            }

            ViewBag.AcademicInternshipId = academic.Id;
            return View(new ProgressForm());
        }

        // STUDENT: Save progress using AcademicInternship ID
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitProgress(
            int id, ProgressForm model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var academic = await _context.AcademicInternships
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.InternshipApplication.StudentUserId == userId &&
                    a.InternshipApplication.Status == "Selected");

            if (academic == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.AcademicInternshipId = id;
                return View(model);
            }

            _context.AcademicProgressUpdates.Add(
                new AcademicProgressUpdate
                {
                    AcademicInternshipId = academic.Id,
                    Description = model.Description.Trim(),
                    SubmittedAt = DateTime.UtcNow
                });
            // Notify assigned faculty about student progress
            if (!string.IsNullOrEmpty(academic.FacultyUserId))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = academic.FacultyUserId,
                    Title = "New Student Progress Update",
                    Message = "An assigned student has submitted a new internship progress update. Please review it.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Progress submitted successfully.";

            return RedirectToAction(
                nameof(SubmitProgress),
                new { id = academic.InternshipApplicationId });
        }

        // COMPANY: Open form using Application ID
        [HttpGet]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> SubmitFeedback(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var application = await _context.InternshipApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Status == "Selected" &&
                    a.InternshipPosting != null &&
                    a.InternshipPosting.CompanyUserId == userId);

            if (application == null) return NotFound();

            var academic = await _context.AcademicInternships
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.InternshipApplicationId == id);

            if (academic == null)
            {
                TempData["StatusMessage"] =
                    "Faculty assignment is required before feedback.";

                return RedirectToAction(
                    "Index", "CompanyApplications",
                    new { id = application.InternshipPostingId });
            }

            ViewBag.AcademicInternshipId = academic.Id;
            return View(new FeedbackForm());
        }

        // COMPANY: Save feedback using AcademicInternship ID
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> SubmitFeedback(
            int id, FeedbackForm model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var academic = await _context.AcademicInternships
                .Include(a => a.InternshipApplication)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.InternshipApplication.Status == "Selected" &&
                    a.InternshipApplication.InternshipPosting != null &&
                    a.InternshipApplication.InternshipPosting
                        .CompanyUserId == userId);

            if (academic == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.AcademicInternshipId = id;
                return View(model);
            }

            _context.AcademicCompanyFeedbacks.Add(
                new AcademicCompanyFeedback
                {
                    AcademicInternshipId = academic.Id,
                    CompanyUserId = userId,
                    Rating = model.Rating,
                    Comments = model.Comments.Trim(),
                    SubmittedAt = DateTime.UtcNow
                });
            // Notify assigned faculty about company feedback
            if (!string.IsNullOrEmpty(academic.FacultyUserId))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = academic.FacultyUserId,
                    Title = "New Company Feedback",
                    Message = "New company feedback has been submitted for an assigned student's internship.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Company feedback submitted successfully.";

            return RedirectToAction(
                nameof(SubmitFeedback),
                new { id = academic.InternshipApplicationId });
        }
        // T-14.3: Faculty - Assigned Students List
        [HttpGet]
        [Authorize(Roles = "FacultySupervisor")]
        public async Task<IActionResult> FacultyStudents()
        {
            var facultyId = _userManager.GetUserId(User);

            var internships = await _context.AcademicInternships
                .AsNoTracking()
                .Where(a => a.FacultyUserId == facultyId)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.Student)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.InternshipPosting)
                .Include(a => a.ProgressUpdates)
                .Include(a => a.CompanyFeedback)
                .Include(a => a.Milestones)
                .AsSplitQuery()
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();

            return View(internships);
        }
        // T-15.1: Faculty Performance Matrix
        [HttpGet]
        [Authorize(Roles = "FacultySupervisor")]
        public async Task<IActionResult> PerformanceMatrix()
        {
            var facultyId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(facultyId))
                return Challenge();

            var internships = await _context.AcademicInternships
                .AsNoTracking()
                .Where(a => a.FacultyUserId == facultyId)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.Student)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.InternshipPosting)
                .Include(a => a.ProgressUpdates)
                .Include(a => a.Milestones)
                .Include(a => a.CompanyFeedback)
                .AsSplitQuery()
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();

            return View(internships);
        }
        // T-14.3: Faculty - Student Progress Details
        [HttpGet]
        [Authorize(Roles = "FacultySupervisor")]
        public async Task<IActionResult> FacultyStudentDetails(int id)
        {
            var facultyId = _userManager.GetUserId(User);

            var internship = await _context.AcademicInternships
                .AsNoTracking()
                .Where(a => a.Id == id && a.FacultyUserId == facultyId)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.Student)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.InternshipPosting)
                .Include(a => a.ProgressUpdates)
                .Include(a => a.CompanyFeedback)
                .Include(a => a.Milestones)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (internship == null)
                return NotFound();

            return View(internship);
        }
    }
}
