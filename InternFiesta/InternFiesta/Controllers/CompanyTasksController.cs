
using System.ComponentModel.DataAnnotations;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyTasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyTasksController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class TaskForm
        {
            [Required]
            [StringLength(150)]
            public string Title { get; set; } = "";

            [Required]
            [StringLength(2000)]
            public string Description { get; set; } = "";

            [DataType(DataType.Date)]
            public DateTime? DueDate { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> SelectedStudents()
        {
            var companyId = _userManager.GetUserId(User);

            if (companyId == null)
                return Challenge();

            var applications = await _context.InternshipApplications
                .AsNoTracking()
                .Where(a =>
                    a.Status == "Selected" &&
                    a.InternshipPosting != null &&
                    a.InternshipPosting.CompanyUserId == companyId)
                .Include(a => a.Student)
                .Include(a => a.InternshipPosting)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var applicationIds = applications
                .Select(a => a.Id)
                .ToList();

            var facultyAssignedIds = await _context.AcademicInternships
                .AsNoTracking()
                .Where(a =>
                    applicationIds.Contains(a.InternshipApplicationId) &&
                    a.FacultyUserId != null &&
                    a.FacultyUserId != "")
                .Select(a => a.InternshipApplicationId)
                .ToListAsync();

            ViewBag.FacultyAssignedIds =
                facultyAssignedIds.ToHashSet();

            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Index(int applicationId)
        {
            var companyId = _userManager.GetUserId(User);
            if (companyId == null) return Challenge();

            var academic = await _context.AcademicInternships
                .AsNoTracking()
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.Student)
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.InternshipPosting)
                .Include(a => a.AssignedTasks)
                .FirstOrDefaultAsync(a =>
                    a.InternshipApplicationId == applicationId &&
                    a.InternshipApplication.Status == "Selected" &&
                    a.InternshipApplication.InternshipPosting != null &&
                    a.InternshipApplication.InternshipPosting.CompanyUserId
                        == companyId);

            if (academic == null)
            {
                TempData["StatusError"] =
                    "The student must be selected and have an assigned faculty supervisor.";

                return RedirectToAction(
                    "Index", "Dashboard");
            }

            ViewBag.Academic = academic;

            return View(new TaskForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int applicationId,
            TaskForm model)
        {
            var companyId = _userManager.GetUserId(User);
            if (companyId == null) return Challenge();

            var academic = await _context.AcademicInternships
                .Include(a => a.InternshipApplication)
                    .ThenInclude(a => a.InternshipPosting)
                .FirstOrDefaultAsync(a =>
                    a.InternshipApplicationId == applicationId &&
                    a.InternshipApplication.Status == "Selected" &&
                    a.InternshipApplication.InternshipPosting != null &&
                    a.InternshipApplication.InternshipPosting.CompanyUserId
                        == companyId);

            if (academic == null) return NotFound();

            if (string.IsNullOrWhiteSpace(academic.FacultyUserId))
                return BadRequest("Faculty assignment is required.");

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(
                    nameof(model.Title), "Task title is required.");

            if (string.IsNullOrWhiteSpace(model.Description))
                ModelState.AddModelError(
                    nameof(model.Description), "Task description is required.");

            if (model.DueDate.HasValue &&
                model.DueDate.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Deadline cannot be in the past.");
            }


            if (!ModelState.IsValid)
            {
                var displayAcademic = await _context.AcademicInternships
                    .AsNoTracking()
                    .Include(a => a.InternshipApplication)
                        .ThenInclude(a => a.Student)
                    .Include(a => a.InternshipApplication)
                        .ThenInclude(a => a.InternshipPosting)
                    .Include(a => a.AssignedTasks)
                    .FirstAsync(a => a.Id == academic.Id);

                ViewBag.Academic = displayAcademic;

                return View("Index", model);
            }


            _context.AssignedTasks.Add(new AssignedTask
            {
                AcademicInternshipId = academic.Id,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                DueDate = model.DueDate,
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                UserId = academic.InternshipApplication.StudentUserId,
                Title = "New Internship Task Assigned",
                Message = $"Your company assigned a new task: {model.Title.Trim()}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] =
                "Task assigned successfully.";

            return RedirectToAction(
                nameof(Index),
                new { applicationId });
        }
    }
}
