
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "FacultySupervisor")]
    public class FacultyEvaluationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly HashSet<string> AllowedGrades =
            new(StringComparer.Ordinal)
            {
                "A+", "A", "A-",
                "B+", "B", "B-",
                "C+", "C", "D", "F"
            };

        public FacultyEvaluationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class EvaluationForm
        {
            [Required(ErrorMessage = "Please select a grade.")]
            public string Grade { get; set; } = "";

            [Required(ErrorMessage = "Evaluation comments are required.")]
            [StringLength(
                2000,
                MinimumLength = 10,
                ErrorMessage = "Write between 10 and 2000 characters.")]
            public string Comments { get; set; } = "";
        }

        private async Task<AcademicInternship?> GetAssignedInternshipAsync(
            int id, string facultyId)
        {
            return await _context.AcademicInternships
                .AsNoTracking()
                .Where(a =>
                    a.Id == id &&
                    a.FacultyUserId == facultyId)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.Student)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.InternshipPosting)
                .Include(a => a.ProgressUpdates)
                .Include(a => a.Milestones)
                .Include(a => a.CompanyFeedback)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        // Open evaluation form
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var facultyId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(facultyId))
                return Challenge();

            var internship = await GetAssignedInternshipAsync(
                id, facultyId);

            if (internship == null)
                return NotFound();

            var evaluation = await _context.FacultyEvaluations
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.AcademicInternshipId == id);

            ViewBag.Internship = internship;
            ViewBag.HasSavedEvaluation = evaluation != null;

            return View(new EvaluationForm
            {
                Grade = evaluation?.Grade ?? "",
                Comments = evaluation?.Comments ?? ""
            });
        }

        // Save or update evaluation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, EvaluationForm model)
        {
            var facultyId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(facultyId))
                return Challenge();

            var internship = await GetAssignedInternshipAsync(
                id, facultyId);

            // A faculty member cannot evaluate another
            // faculty member's assigned student.
            if (internship == null)
                return NotFound();

            if (!AllowedGrades.Contains(model.Grade ?? ""))
            {
                ModelState.AddModelError(
                    nameof(model.Grade),
                    "Please select a valid grade.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Internship = internship;

                ViewBag.HasSavedEvaluation =
                    await _context.FacultyEvaluations
                        .AnyAsync(e =>
                            e.AcademicInternshipId == id);

                return View(model);
            }

            var evaluation = await _context.FacultyEvaluations
                .FirstOrDefaultAsync(e =>
                    e.AcademicInternshipId == id);

            bool isNewEvaluation = evaluation == null;

            if (evaluation == null)
            {
                evaluation = new FacultyEvaluation
                {
                    AcademicInternshipId = id
                };

                _context.FacultyEvaluations.Add(evaluation);
            }

            evaluation.FacultyUserId = facultyId;
            evaluation.Grade = model.Grade ?? string.Empty;
            evaluation.Comments = model.Comments.Trim();
            evaluation.EvaluatedAt = DateTime.UtcNow;

            // Notify the student on first evaluation.
            if (isNewEvaluation)
            {
                var studentId =
                    internship.InternshipApplication.StudentUserId;

                if (!string.IsNullOrWhiteSpace(studentId))
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = studentId,
                        Title = "Faculty Evaluation Completed",
                        Message =
                            "Your faculty supervisor has completed " +
                            "your academic internship evaluation.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Faculty evaluation saved successfully.";

            return RedirectToAction(
                nameof(Summary),
                new { id });
        }

        // Printable evaluation report
        [HttpGet]
        public async Task<IActionResult> Summary(int id)
        {
            var facultyId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(facultyId))
                return Challenge();

            var evaluation = await _context.FacultyEvaluations
                .AsNoTracking()
                .Where(e =>
                    e.AcademicInternshipId == id &&
                    e.AcademicInternship.FacultyUserId == facultyId)
                .Include(e => e.AcademicInternship)
                    .ThenInclude(a => a.InternshipApplication)
                        .ThenInclude(a => a.Student)
                .Include(e => e.AcademicInternship)
                    .ThenInclude(a => a.InternshipApplication)
                        .ThenInclude(a => a.InternshipPosting)
                .Include(e => e.AcademicInternship)
                    .ThenInclude(a => a.FacultyUser)
                .Include(e => e.AcademicInternship)
                    .ThenInclude(a => a.ProgressUpdates)
                .Include(e => e.AcademicInternship)
                    .ThenInclude(a => a.Milestones)
                .Include(e => e.AcademicInternship)
                    .ThenInclude(a => a.CompanyFeedback)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (evaluation == null)
                return NotFound();

            return View(evaluation);
        }
    }
}
