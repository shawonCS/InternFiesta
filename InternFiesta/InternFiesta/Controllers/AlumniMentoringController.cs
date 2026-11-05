
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.ViewModels.Alumni;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternFiesta.Controllers
{
    [Authorize]
    public class AlumniMentoringController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlumniMentoringController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Only profiles made visible by their owners
        // are accessible to students.
        private IQueryable<AlumniPublicProfile> VisibleProfiles()
        {
            return _context.AlumniProfiles
                .AsNoTracking()
                .Where(p => p.IsVisibleToStudents)
                .Select(p => new AlumniPublicProfile
                {
                    Id = p.Id,
                    FullName = p.User.FullName,

                    // Professional Headline removed.
                    Industry = p.Industry,
                    JobTitle = p.JobTitle,
                    CompanyName = p.CompanyName,
                    About = p.About,
                    Skills = p.Skills,
                    LinkedInUrl = p.LinkedInUrl,

                    IsAvailableForMentoring =
                        p.IsAvailableForMentoring
                });
        }

        // STUDENT: Browse visible alumni profiles.
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Directory(string? q)
        {
            var query = VisibleProfiles();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();

                query = query.Where(p =>
                    p.FullName.Contains(search) ||
                    p.Industry.Contains(search) ||
                    (p.JobTitle != null &&
                     p.JobTitle.Contains(search)) ||
                    (p.CompanyName != null &&
                     p.CompanyName.Contains(search)));
            }

            var alumni = await query
                .OrderBy(p => p.FullName)
                .ToListAsync();

            ViewBag.Search = q;

            return View(alumni);
        }

        // STUDENT: View one visible alumni profile.
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Profile(int id)
        {
            var profile = await VisibleProfiles()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
                return NotFound();

            var studentId = CurrentUserId;

            if (studentId == null)
                return Challenge();

            var currentRequestStatus =
                await _context.AlumniMentoringRequests
                    .AsNoTracking()
                    .Where(r =>
                        r.AlumniProfileId == id &&
                        r.StudentUserId == studentId &&
                        (r.Status == "Pending" ||
                         r.Status == "Accepted"))
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Select(r => r.Status)
                    .FirstOrDefaultAsync();

            ViewBag.RequestStatus = currentRequestStatus;

            return View(profile);
        }

        // STUDENT: Send a mentoring request.
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(
            int id,
            string message)
        {
            var studentId = CurrentUserId;

            if (studentId == null)
                return Challenge();

            var cleanedMessage = message?.Trim() ?? "";

            // Validate request message.
            if (cleanedMessage.Length < 20 ||
                cleanedMessage.Length > 800)
            {
                TempData["ErrorMessage"] =
                    "Please write a message between " +
                    "20 and 800 characters.";

                return RedirectToAction(
                    nameof(Profile), new { id });
            }

            // Mentor must be visible and available.
            var mentor = await _context.AlumniProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.IsVisibleToStudents &&
                    p.IsAvailableForMentoring);

            if (mentor == null)
                return NotFound();

            // Prevent repeated active requests.
            bool alreadyRequested =
                await _context.AlumniMentoringRequests
                    .AnyAsync(r =>
                        r.AlumniProfileId == id &&
                        r.StudentUserId == studentId &&
                        (r.Status == "Pending" ||
                         r.Status == "Accepted"));

            if (alreadyRequested)
            {
                TempData["WarningMessage"] =
                    "You already have an active mentoring request.";

                return RedirectToAction(
                    nameof(Profile), new { id });
            }

            // New mentoring request.
            var request = new AlumniMentoringRequest
            {
                AlumniProfileId = id,
                StudentUserId = studentId,
                Message = cleanedMessage,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.AlumniMentoringRequests.Add(request);

            // Notify the alumni.
            _context.Notifications.Add(new Notification
            {
                UserId = mentor.UserId,
                Title = "New Mentoring Request",
                Message =
                    "A student has requested career mentoring.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Check whether a concurrent request
                // already created the same pending request.
                bool duplicate =
                    await _context.AlumniMentoringRequests
                        .AsNoTracking()
                        .AnyAsync(r =>
                            r.AlumniProfileId == id &&
                            r.StudentUserId == studentId &&
                            r.Status == "Pending");

                if (!duplicate)
                    throw;

                TempData["WarningMessage"] =
                    "Your mentoring request is already pending.";

                return RedirectToAction(
                    nameof(Profile), new { id });
            }

            TempData["SuccessMessage"] =
                "Your mentoring request has been sent.";

            return RedirectToAction(
                nameof(Profile), new { id });
        }

        // ALUMNI: View requests sent to own profile.
        [HttpGet]
        [Authorize(Roles = "Alumni")]
        public async Task<IActionResult> Inbox()
        {
            var alumniUserId = CurrentUserId;

            if (alumniUserId == null)
                return Challenge();

            var requests =
                await _context.AlumniMentoringRequests
                    .AsNoTracking()
                    .Include(r => r.StudentUser)
                    .Where(r =>
                        r.AlumniProfile.UserId == alumniUserId)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .ToListAsync();

            return View(requests);
        }

        // ALUMNI: Accept or decline own request.
        [HttpPost]
        [Authorize(Roles = "Alumni")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(
            int id,
            string decision)
        {
            if (decision != "Accepted" &&
                decision != "Declined")
            {
                return BadRequest("Invalid decision.");
            }

            var alumniUserId = CurrentUserId;

            if (alumniUserId == null)
                return Challenge();

            // Alumni can modify only requests
            // sent to their own profile.
            var request =
                await _context.AlumniMentoringRequests
                    .FirstOrDefaultAsync(r =>
                        r.Id == id &&
                        r.AlumniProfile.UserId == alumniUserId);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
            {
                TempData["WarningMessage"] =
                    "This request has already been reviewed.";

                return RedirectToAction(nameof(Inbox));
            }

            request.Status = decision;
            request.RespondedAtUtc = DateTime.UtcNow;

            // Notify the student.
            _context.Notifications.Add(new Notification
            {
                UserId = request.StudentUserId,
                Title = "Mentoring Request " + decision,
                Message =
                    "Your mentoring request was " +
                    decision.ToLowerInvariant() + ".",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Mentoring request " +
                decision.ToLowerInvariant() + ".";

            return RedirectToAction(nameof(Inbox));
        }
    }
}
