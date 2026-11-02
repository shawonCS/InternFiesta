using InternFiesta.Services;
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
        private readonly UploadFileStorage? _uploadStorage;

        public AcademicProgressController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            UploadFileStorage? uploadStorage = null)
        {
            _context = context;
            _userManager = userManager;
            _uploadStorage = uploadStorage;
        }


        public class ProgressForm
        {
            [Range(1, int.MaxValue, ErrorMessage = "Please select an assigned task.")]
            public int AssignedTaskId { get; set; }
            [Required]
            [StringLength(2000)]
            public string Description { get; set; } = "";
            public List<IFormFile> EvidenceFiles { get; set; } = new();
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

        // STUDENT: View own internship tasks
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyTasks()
        {
            var studentId = _userManager.GetUserId(User);

            if (studentId == null)
                return Challenge();

            var selectedApplications =
                await _context.InternshipApplications
                    .AsNoTracking()
                    .Where(a =>
                        a.StudentUserId == studentId &&
                        a.Status == "Selected")
                    .Include(a => a.InternshipPosting)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

            var selectedIds = selectedApplications
                .Select(a => a.Id)
                .ToList();

            var academicInternships =
                await _context.AcademicInternships
                    .AsNoTracking()
                    .Where(a =>
                        selectedIds.Contains(a.InternshipApplicationId))
                    .Include(a => a.AssignedTasks)
                    .ToListAsync();

            ViewBag.AcademicInternships = academicInternships;

            return View(selectedApplications);
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
                .Include(a => a.AssignedTasks)
                .FirstOrDefaultAsync(a =>
                    a.InternshipApplicationId == id &&
                    a.InternshipApplication.StudentUserId == userId &&
                    a.InternshipApplication.Status == "Selected" &&
                    a.FacultyUserId != "" &&
                    a.AssignedTasks.Any(t => t.Status != "Completed"));

            if (academic == null)
            {
                TempData["StatusMessage"] =
                    "Faculty assignment and an active company task are required before submitting progress.";

                return RedirectToAction(
                    "MyApplications", "StudentInternships");
            }

            ViewBag.AcademicInternshipId = academic.Id;

            ViewBag.AssignedTasks = academic.AssignedTasks
                .Where(t => t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .ToList();

            ViewBag.ProgressHistory = await LoadProgressHistoryAsync(academic.Id);
            return View(new ProgressForm());
        }

        // STUDENT: Save progress using AcademicInternship ID
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitProgress(int id, ProgressForm model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var academic = await _context.AcademicInternships
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.InternshipApplication.StudentUserId == userId &&
                    a.InternshipApplication.Status == "Selected");

            if (academic == null) return NotFound();

            if (string.IsNullOrWhiteSpace(academic.FacultyUserId))
                return Forbid();

            var assignedTask = await _context.AssignedTasks
                .FirstOrDefaultAsync(t =>
                    t.Id == model.AssignedTaskId &&
                    t.AcademicInternshipId == academic.Id &&
                    t.Status != "Completed");

            if (assignedTask == null)
            {
                ModelState.AddModelError(
                    nameof(ProgressForm.AssignedTaskId),
                    "Please select a valid assigned task.");
            }

            var files = model.EvidenceFiles ?? new List<IFormFile>();
            if (files.Count > UploadFileStorage.MaxEvidenceFiles)
            {
                ModelState.AddModelError(nameof(ProgressForm.EvidenceFiles),
                    "Upload a maximum of 3 supporting documents.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AcademicInternshipId = id;
                ViewBag.ProgressHistory = await LoadProgressHistoryAsync(academic.Id);
                ViewBag.AssignedTasks = await _context.AssignedTasks
                .AsNoTracking()
                .Where(t =>
                    t.AcademicInternshipId == academic.Id &&
                    t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .ToListAsync();
                return View(model);
            }

            var savedFiles = new List<string>();
            try
            {

                var progress = new AcademicProgressUpdate
                {
                    AcademicInternshipId = academic.Id,
                    AssignedTaskId = assignedTask!.Id,
                    Description = model.Description.Trim(),
                    SubmittedAt = DateTime.UtcNow
                };

                foreach (var file in files)
                {
                    if (_uploadStorage == null)
                        throw new InvalidOperationException("Upload storage is not registered.");

                    var saved = await _uploadStorage.SaveEvidenceAsync(
                        file, HttpContext.RequestAborted);
                    savedFiles.Add(saved.StoredFileName);

                    progress.Attachments.Add(new AcademicProgressAttachment
                    {
                        OriginalFileName = saved.OriginalFileName,
                        StoredFileName = saved.StoredFileName,
                        ContentType = saved.ContentType,
                        FileSizeBytes = saved.FileSizeBytes,
                        UploadedAtUtc = DateTime.UtcNow
                    });
                }

                _context.AcademicProgressUpdates.Add(progress);
                assignedTask!.Status = "Submitted";
                // Keep existing faculty notification.
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
            }
            catch (InvalidDataException ex)
            {
                foreach (var name in savedFiles)
                    _uploadStorage?.DeleteEvidence(name);

                ModelState.AddModelError(nameof(ProgressForm.EvidenceFiles), ex.Message);
                ViewBag.AcademicInternshipId = id;
                ViewBag.ProgressHistory = await LoadProgressHistoryAsync(academic.Id);
                ViewBag.AssignedTasks = await _context.AssignedTasks
                .AsNoTracking()
                .Where(t =>
                    t.AcademicInternshipId == academic.Id &&
                    t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .ToListAsync();
                return View(model);
            }
            catch
            {
                foreach (var name in savedFiles)
                    _uploadStorage?.DeleteEvidence(name);
                throw;
            }

            TempData["SuccessMessage"] = "Progress and supporting documents submitted successfully.";
            return RedirectToAction(nameof(SubmitProgress),
                new { id = academic.InternshipApplicationId });
        }

        private Task<List<AcademicProgressUpdate>> LoadProgressHistoryAsync(int academicId)
        {
            return _context.AcademicProgressUpdates.AsNoTracking()
                .Where(p => p.AcademicInternshipId == academicId)
                .Include(p => p.Attachments)
                .Include(p => p.AssignedTask)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();
        }

        [HttpGet]
        [Authorize(Roles = "Student,FacultySupervisor")]
        public async Task<IActionResult> DownloadEvidence(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var attachment = await _context.AcademicProgressAttachments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.StoredFileName,
                    a.OriginalFileName,
                    StudentId = a.ProgressUpdate.AcademicInternship
                        .InternshipApplication.StudentUserId,
                    FacultyId = a.ProgressUpdate.AcademicInternship.FacultyUserId
                })
                .FirstOrDefaultAsync();

            if (attachment == null) return NotFound();
            if (attachment.StudentId != userId && attachment.FacultyId != userId)
                return Forbid();

            var path = _uploadStorage?.EvidencePath(attachment.StoredFileName);
            if (path == null || !System.IO.File.Exists(path)) return NotFound();

            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(path, "application/octet-stream", attachment.OriginalFileName);
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
                .Include(a => a.AssignedTasks)
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
                .Include(a => a.AssignedTasks)
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
                    .ThenInclude(p => p.Attachments)
                .Include(a => a.ProgressUpdates)
                    .ThenInclude(p => p.AssignedTask)
                .Include(a => a.CompanyFeedback)
                .Include(a => a.Milestones)
                .Include(a => a.AssignedTasks)

                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (internship == null)
                return NotFound();

            return View(internship);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "FacultySupervisor")]
        public async Task<IActionResult> CompleteTask(int taskId)
        {
            var facultyId = _userManager.GetUserId(User);
            if (facultyId == null) return Challenge();

            var task = await _context.AssignedTasks
                .Include(t => t.AcademicInternship)
                    .ThenInclude(a => a.InternshipApplication)
                .FirstOrDefaultAsync(t =>
                    t.Id == taskId &&
                    t.AcademicInternship.FacultyUserId == facultyId &&
                    t.AcademicInternship.InternshipApplication.Status
                        == "Selected");

            if (task == null) return NotFound();

            if (task.Status != "Submitted")
            {
                TempData["StatusMessage"] =
                    "Only submitted tasks can be marked completed.";

                return RedirectToAction(
                    nameof(FacultyStudentDetails),
                    new { id = task.AcademicInternshipId });
            }

            bool hasProgress = await _context.AcademicProgressUpdates
                .AnyAsync(p =>
                    p.AssignedTaskId == task.Id &&
                    p.AcademicInternshipId == task.AcademicInternshipId);

            if (!hasProgress)
                return BadRequest("No submitted progress found for this task.");

            task.Status = "Completed";
            task.CompletedAt = DateTime.UtcNow;

            _context.Notifications.Add(new Notification
            {
                UserId = task.AcademicInternship
                    .InternshipApplication.StudentUserId,
                Title = "Internship Task Completed",
                Message = $"Your task '{task.Title}' has been approved as completed.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] =
                "Task marked completed successfully.";

            return RedirectToAction(
                nameof(FacultyStudentDetails),
                new { id = task.AcademicInternshipId });
        }

    }
}
