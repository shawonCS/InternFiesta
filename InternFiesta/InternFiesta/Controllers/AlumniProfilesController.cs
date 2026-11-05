
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.ViewModels.Alumni;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Alumni")]
    public class AlumniProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AlumniProfilesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // MY PROFILE
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var profile = await _context.AlumniProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            ViewBag.FullName = user.FullName;

            return View(profile);
        }

        // EDIT PROFILE - LOAD EXISTING VALUES
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var existing = await _context.AlumniProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var model = existing == null
                ? new AlumniProfileForm()
                : new AlumniProfileForm
                {
                    Industry = existing.Industry,
                    JobTitle = existing.JobTitle,
                    CompanyName = existing.CompanyName,
                    About = existing.About,
                    Skills = existing.Skills,
                    LinkedInUrl = existing.LinkedInUrl,

                    // Each setting loads independently.
                    IsVisibleToStudents =
                        existing.IsVisibleToStudents,

                    IsAvailableForMentoring =
                        existing.IsAvailableForMentoring
                };

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            return View(model);
        }

        // EDIT PROFILE - SAVE CHANGES
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AlumniProfileForm model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // LinkedIn is optional.
            // If provided, validate the URL.
            if (!string.IsNullOrWhiteSpace(model.LinkedInUrl) &&
                !IsSecureLinkedInUrl(model.LinkedInUrl))
            {
                ModelState.AddModelError(
                    nameof(model.LinkedInUrl),
                    "Enter a valid HTTPS LinkedIn URL.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FullName = user.FullName;
                ViewBag.Email = user.Email;

                return View(model);
            }

            // Security: Profile ownership comes from
            // the logged-in Identity user, not form input.
            var profile = await _context.AlumniProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            // Support older Alumni accounts that
            // do not yet have a profile record.
            if (profile == null)
            {
                profile = new AlumniProfile
                {
                    UserId = user.Id,

                    // Legacy non-nullable column.
                    // No longer shown in the UI.
                    Headline = string.Empty
                };

                _context.AlumniProfiles.Add(profile);
            }

            // OPTIONAL PROFESSIONAL INFORMATION
            // Industry is non-nullable in the current DB.
            profile.Industry =
                model.Industry?.Trim() ?? string.Empty;

            profile.JobTitle = Clean(model.JobTitle);
            profile.CompanyName = Clean(model.CompanyName);
            profile.About = Clean(model.About);
            profile.Skills = Clean(model.Skills);
            profile.LinkedInUrl = Clean(model.LinkedInUrl);

            // SETTING 1: PROFILE VISIBILITY
            // Saved independently.
            profile.IsVisibleToStudents =
                model.IsVisibleToStudents;

            // SETTING 2: MENTORING AVAILABILITY
            // Saved independently.
            profile.IsAvailableForMentoring =
                model.IsAvailableForMentoring;

            profile.UpdatedAtUtc = DateTime.UtcNow;

            // Persist profile information and settings.
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Profile updated successfully.";

            // Return to My Profile after saving.
            return RedirectToAction(nameof(Index));
        }

        // Remove whitespace from optional fields.
        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        // Allow only HTTPS LinkedIn URLs.
        private static bool IsSecureLinkedInUrl(string value)
        {
            if (!Uri.TryCreate(
                value.Trim(),
                UriKind.Absolute,
                out var uri))
            {
                return false;
            }

            bool validScheme =
                uri.Scheme == Uri.UriSchemeHttps;

            bool validHost =
                uri.Host.Equals(
                    "linkedin.com",
                    StringComparison.OrdinalIgnoreCase)
                ||
                uri.Host.EndsWith(
                    ".linkedin.com",
                    StringComparison.OrdinalIgnoreCase);

            return validScheme && validHost;
        }
    }
}
