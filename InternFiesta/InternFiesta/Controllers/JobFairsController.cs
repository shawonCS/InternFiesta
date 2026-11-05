
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Student,Company,UniversityCoordinator")]
    public class JobFairsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public JobFairsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        public class DetailsPage
        {
            public JobFair Fair { get; set; } = null!;
            public JobFairRegistration? MyRegistration { get; set; }
            public List<JobFairBooth> Booths { get; set; } = new();
            public bool HasFeedback { get; set; }
        }

        public class AttendancePage
        {
            public JobFair Fair { get; set; } = null!;
            public List<JobFairRegistration> Registrations { get; set; } = new();
        }

        public class BoothForm
        {
            [Required, StringLength(150)]
            public string BoothName { get; set; } = "";

            [Required, StringLength(1000)]
            public string Description { get; set; } = "";

            [Required, EmailAddress, StringLength(200)]
            public string ContactEmail { get; set; } = "";

            [StringLength(100)]
            public string? BoothLocation { get; set; }
        }

        public class FeedbackForm
        {
            [Range(1, 5)]
            public int Rating { get; set; } = 5;

            [Required, StringLength(1000)]
            public string Comments { get; set; } = "";
        }

        // T-17.1: Show available events
        [HttpGet]
        public Task<IActionResult> Index()
        {
            return ShowEvents(false);
        }

        // T-17.1: Show only events registered by this user
        [HttpGet]
        public Task<IActionResult> MyEvents()
        {
            return ShowEvents(true);
        }

        private async Task<IActionResult> ShowEvents(bool onlyMine)
        {
            var userId = CurrentUserId;
            if (userId == null) return Challenge();

            var registeredIds = await _db.JobFairRegistrations
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(r => r.JobFairId)
                .ToListAsync();

            var query = _db.JobFairs.AsNoTracking();

            if (onlyMine)
            {
                query = query.Where(f => registeredIds.Contains(f.Id));
            }

            var fairs = await query
                .OrderByDescending(f => f.StartsAtUtc)
                .ToListAsync();

            ViewBag.OnlyMine = onlyMine;
            ViewBag.RegisteredIds = registeredIds.ToHashSet();

            return View("Index", fairs);
        }

        // T-17.1 + T-17.2: Event details and companies
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var fair = await _db.JobFairs
                .AsNoTracking()
                .Include(f => f.Registrations)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fair == null) return NotFound();

            var mine = fair.Registrations
                .FirstOrDefault(r => r.UserId == CurrentUserId);

            var booths = await _db.JobFairBooths
                .AsNoTracking()
                .Where(b => b.JobFairId == id)
                .ToListAsync();

            var hasFeedback = mine != null &&
                await _db.JobFairFeedbacks
                    .AnyAsync(f => f.JobFairRegistrationId == mine.Id);

            return View(new DetailsPage
            {
                Fair = fair,
                MyRegistration = mine,
                Booths = booths,
                HasFeedback = hasFeedback
            });
        }

        // T-17.1: Register Student or Company
        [HttpPost]
        [Authorize(Roles = "Student,Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            var userId = CurrentUserId;
            if (userId == null) return Challenge();

            var fair = await _db.JobFairs.FindAsync(id);
            if (fair == null) return NotFound();

            var alreadyRegistered = await _db.JobFairRegistrations
                .AnyAsync(r => r.JobFairId == id && r.UserId == userId);

            if (alreadyRegistered)
            {
                TempData["WarningMessage"] =
                    "You are already registered for this event.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (fair.IsClosed || fair.EndsAtUtc <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] =
                    "Registration is closed for this job fair.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var role = User.IsInRole("Student")
                ? "Student" : "Company";

            _db.JobFairRegistrations.Add(
                new JobFairRegistration
                {
                    JobFairId = id,
                    UserId = userId,
                    ParticipantRole = role,
                    RegisteredAtUtc = DateTime.UtcNow
                });
            // T-18.3: Registration notification
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Job Fair Registration Confirmed",
                Message = $"You have successfully registered for '{fair.Title}'.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Handle a second registration request
                // racing with the first request.
                var duplicate = await _db.JobFairRegistrations
                    .AsNoTracking()
                    .AnyAsync(r =>
                        r.JobFairId == id &&
                        r.UserId == userId);

                if (!duplicate) throw;

                TempData["WarningMessage"] =
                    "You are already registered for this event.";

                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] =
                "Registration successful!";

            return RedirectToAction(nameof(Details), new { id });
        }

        // T-17.2: Company booth form
        [HttpGet]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Booth(int id)
        {
            var fair = await _db.JobFairs.FindAsync(id);
            if (fair == null) return NotFound();

            var userId = CurrentUserId;
            if (userId == null) return Challenge();

            var registered = await _db.JobFairRegistrations
                .AnyAsync(r =>
                    r.JobFairId == id &&
                    r.UserId == userId &&
                    r.ParticipantRole == "Company");

            if (!registered) return Forbid();

            if (fair.IsClosed || fair.EndsAtUtc <= DateTime.UtcNow)
            {
                TempData["WarningMessage"] =
                    "Booth editing is closed.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var booth = await _db.JobFairBooths
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.JobFairId == id &&
                    b.CompanyUserId == userId);

            ViewBag.JobFairId = id;
            ViewBag.JobFairTitle = fair.Title;

            return View(new BoothForm
            {
                BoothName = booth?.BoothName ?? "",
                Description = booth?.Description ?? "",
                ContactEmail = booth?.ContactEmail ?? "",
                BoothLocation = booth?.BoothLocation
            });
        }

        // T-17.2: Save or update booth information
        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booth(int id, BoothForm form)
        {
            var userId = CurrentUserId;
            if (userId == null) return Challenge();

            var fair = await _db.JobFairs.FindAsync(id);
            if (fair == null) return NotFound();

            var registered = await _db.JobFairRegistrations
                .AnyAsync(r =>
                    r.JobFairId == id &&
                    r.UserId == userId &&
                    r.ParticipantRole == "Company");

            if (!registered) return Forbid();

            if (fair.IsClosed || fair.EndsAtUtc <= DateTime.UtcNow)
                return BadRequest("Booth editing is closed.");

            if (!ModelState.IsValid)
            {
                ViewBag.JobFairId = id;
                ViewBag.JobFairTitle = fair.Title;
                return View(form);
            }

            var booth = await _db.JobFairBooths
                .FirstOrDefaultAsync(b =>
                    b.JobFairId == id &&
                    b.CompanyUserId == userId);

            if (booth == null)
            {
                booth = new JobFairBooth
                {
                    JobFairId = id,
                    CompanyUserId = userId
                };

                _db.JobFairBooths.Add(booth);
            }

            booth.BoothName = form.BoothName.Trim();
            booth.Description = form.Description.Trim();
            booth.ContactEmail = form.ContactEmail.Trim();
            booth.BoothLocation = form.BoothLocation?.Trim();

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Company booth information saved.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // T-17.3: Coordinator sees registered participants
        [HttpGet]
        [Authorize(Roles = "UniversityCoordinator")]
        public async Task<IActionResult> Attendance(int id)
        {
            var fair = await _db.JobFairs
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fair == null) return NotFound();

            var registrations = await _db.JobFairRegistrations
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.JobFairId == id)
                .OrderBy(r => r.ParticipantRole)
                .ThenBy(r => r.RegisteredAtUtc)
                .ToListAsync();

            return View(new AttendancePage
            {
                Fair = fair,
                Registrations = registrations
            });
        }

        // T-17.3: Coordinator marks attendance
        [HttpPost]
        [Authorize(Roles = "UniversityCoordinator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(int registrationId)
        {
            var registration = await _db.JobFairRegistrations
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            if (registration == null) return NotFound();

            var fair = await _db.JobFairs
                .FindAsync(registration.JobFairId);

            if (fair == null) return NotFound();

            if (DateTime.UtcNow < fair.StartsAtUtc)
            {
                TempData["WarningMessage"] =
                    "Attendance can be marked after the event starts.";

                return RedirectToAction(
                    nameof(Attendance),
                    new { id = fair.Id });
            }

            if (registration.AttendedAtUtc == null)
            {
                registration.AttendedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Attendance marked successfully.";

            return RedirectToAction(
                nameof(Attendance),
                new { id = fair.Id });
        }

        // T-17.3: Feedback form after event ends
        [HttpGet]
        [Authorize(Roles = "Student,Company")]
        public async Task<IActionResult> Feedback(int id)
        {
            var userId = CurrentUserId;
            if (userId == null) return Challenge();

            var fair = await _db.JobFairs.FindAsync(id);
            if (fair == null) return NotFound();

            var registration = await _db.JobFairRegistrations
                .FirstOrDefaultAsync(r =>
                    r.JobFairId == id &&
                    r.UserId == userId);

            if (registration == null) return Forbid();

            if (DateTime.UtcNow < fair.EndsAtUtc)
            {
                TempData["WarningMessage"] =
                    "Feedback is available after the event ends.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var saved = await _db.JobFairFeedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.JobFairRegistrationId == registration.Id);

            ViewBag.JobFairId = id;
            ViewBag.JobFairTitle = fair.Title;

            return View(new FeedbackForm
            {
                Rating = saved?.Rating ?? 5,
                Comments = saved?.Comments ?? ""
            });
        }

        // T-17.3: Submit or update feedback
        [HttpPost]
        [Authorize(Roles = "Student,Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(int id, FeedbackForm form)
        {
            var userId = CurrentUserId;
            if (userId == null) return Challenge();

            var fair = await _db.JobFairs.FindAsync(id);
            if (fair == null) return NotFound();

            var registration = await _db.JobFairRegistrations
                .FirstOrDefaultAsync(r =>
                    r.JobFairId == id &&
                    r.UserId == userId);

            if (registration == null) return Forbid();

            if (DateTime.UtcNow < fair.EndsAtUtc)
                return BadRequest("The event has not ended yet.");

            if (!ModelState.IsValid)
            {
                ViewBag.JobFairId = id;
                ViewBag.JobFairTitle = fair.Title;
                return View(form);
            }

            var feedback = await _db.JobFairFeedbacks
                .FirstOrDefaultAsync(f =>
                    f.JobFairRegistrationId == registration.Id);

            if (feedback == null)
            {
                feedback = new JobFairFeedback
                {
                    JobFairRegistrationId = registration.Id
                };

                _db.JobFairFeedbacks.Add(feedback);
            }

            feedback.Rating = form.Rating;
            feedback.Comments = form.Comments.Trim();
            feedback.SubmittedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Thank you! Your feedback has been saved.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
