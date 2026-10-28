using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "UniversityCoordinator")]
    public class CoordinatorJobFairsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoordinatorJobFairsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class JobFairForm
        {
            [Required(ErrorMessage = "Enter an event title.")]
            [StringLength(150)]
            public string Title { get; set; } = "";

            [Required(ErrorMessage = "Enter a description.")]
            [StringLength(2000)]
            public string Description { get; set; } = "";

            [Required(ErrorMessage = "Enter an event venue.")]
            [StringLength(200)]
            public string Venue { get; set; } = "";

            [Display(Name = "Start Date & Time")]
            public DateTime StartsAt { get; set; }

            [Display(Name = "End Date & Time")]
            public DateTime EndsAt { get; set; }
        }

        private void ValidateDates(JobFairForm model, bool isCreate)
        {
            if (model.StartsAt == default)
            {
                ModelState.AddModelError(
                    nameof(model.StartsAt),
                    "Select the start date and time.");
            }

            if (model.EndsAt == default)
            {
                ModelState.AddModelError(
                    nameof(model.EndsAt),
                    "Select the end date and time.");
            }

            if (model.EndsAt <= model.StartsAt)
            {
                ModelState.AddModelError(
                    nameof(model.EndsAt),
                    "End time must be later than start time.");
            }

            if (isCreate &&
                model.StartsAt <= DateTime.UtcNow.AddHours(6))
            {
                ModelState.AddModelError(
                    nameof(model.StartsAt),
                    "Start time must be in the future.");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(
                    nameof(model.Title), "Title is required.");

            if (string.IsNullOrWhiteSpace(model.Description))
                ModelState.AddModelError(
                    nameof(model.Description), "Description is required.");

            if (string.IsNullOrWhiteSpace(model.Venue))
                ModelState.AddModelError(
                    nameof(model.Venue), "Venue is required.");
        }

        private static DateTime ToUtc(DateTime bangladeshTime)
        {
            return DateTime.SpecifyKind(
                bangladeshTime.AddHours(-6),
                DateTimeKind.Utc);
        }

        // T-16.2: Coordinator event list and registration counts
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var fairs = await _context.JobFairs
                .AsNoTracking()
                .Include(f => f.Registrations)
                .OrderByDescending(f => f.StartsAtUtc)
                .ToListAsync();

            return View(fairs);
        }

        // T-16.1: Create form
        [HttpGet]
        public IActionResult Create()
        {
            var start = DateTime.UtcNow.AddHours(6).AddDays(7);

            ViewBag.IsEdit = false;

            return View(new JobFairForm
            {
                StartsAt = start,
                EndsAt = start.AddHours(5)
            });
        }

        // T-16.1: Save new job fair
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobFairForm model)
        {
            ValidateDates(model, true);

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = false;
                return View(model);
            }

            var coordinatorId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(coordinatorId))
                return Challenge();

            var fair = new JobFair
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Venue = model.Venue.Trim(),
                StartsAtUtc = ToUtc(model.StartsAt),
                EndsAtUtc = ToUtc(model.EndsAt),
                CreatedByCoordinatorId = coordinatorId,
                CreatedAtUtc = DateTime.UtcNow,
                IsClosed = false
            };

            _context.JobFairs.Add(fair);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job fair created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // T-16.1: Edit form
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var fair = await _context.JobFairs
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fair == null)
                return NotFound();

            if (fair.IsClosed)
            {
                TempData["WarningMessage"] =
                    "Closed job fairs cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsEdit = true;
            ViewBag.EditId = id;

            return View("Create", new JobFairForm
            {
                Title = fair.Title,
                Description = fair.Description,
                Venue = fair.Venue,
                StartsAt = fair.StartsAtUtc.AddHours(6),
                EndsAt = fair.EndsAtUtc.AddHours(6)
            });
        }

        // T-16.1: Update job fair
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobFairForm model)
        {
            var fair = await _context.JobFairs
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fair == null)
                return NotFound();

            if (fair.IsClosed)
                return BadRequest("Closed job fairs cannot be edited.");

            ValidateDates(model, false);

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                ViewBag.EditId = id;

                return View("Create", model);
            }

            fair.Title = model.Title.Trim();
            fair.Description = model.Description.Trim();
            fair.Venue = model.Venue.Trim();
            fair.StartsAtUtc = ToUtc(model.StartsAt);
            fair.EndsAtUtc = ToUtc(model.EndsAt);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job fair updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // T-16.1: Close job fair
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var fair = await _context.JobFairs
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fair == null)
                return NotFound();


            if (!fair.IsClosed)
            {
                // T-18.3: Get registered users only
                var userIds = await _context.JobFairRegistrations
                    .Where(r => r.JobFairId == id)
                    .Select(r => r.UserId)
                    .Distinct()
                    .ToListAsync();

                // Close the Job Fair
                fair.IsClosed = true;

                // Notify registered users
                foreach (var userId in userIds)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Title = "Job Fair Closed",
                        Message = $"The job fair '{fair.Title}' has been closed.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }


            TempData["SuccessMessage"] =
                "Job fair closed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}