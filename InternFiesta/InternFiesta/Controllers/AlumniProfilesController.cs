
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

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Edit));
        }

        // Open only the logged-in alumnus's profile.
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
                    Headline = existing.Headline,
                    Industry = existing.Industry,
                    JobTitle = existing.JobTitle,
                    CompanyName = existing.CompanyName,
                    About = existing.About,
                    Skills = existing.Skills,
                    LinkedInUrl = existing.LinkedInUrl,
                    IsVisibleToStudents =
                        existing.IsVisibleToStudents,
                    IsAvailableForMentoring =
                        existing.IsAvailableForMentoring
                };

            ViewBag.FullName = user.FullName;
            ViewBag.ProfileCompletion = CalculateCompletion(model);

            return View(model);
        }

        // Create or update own profile.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AlumniProfileForm model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Profile discoverability is needed for students
            // to find an available mentor.
            if (model.IsAvailableForMentoring &&
                !model.IsVisibleToStudents)
            {
                ModelState.AddModelError(
                    nameof(model.IsVisibleToStudents),
                    "Enable profile visibility to offer mentoring.");
            }

            // Only allow valid HTTPS LinkedIn profile links.
            if (!string.IsNullOrWhiteSpace(model.LinkedInUrl))
            {
                var validUrl = Uri.TryCreate(
                    model.LinkedInUrl.Trim(),
                    UriKind.Absolute,
                    out var uri);

                bool validHost = validUrl &&
                    uri != null &&
                    uri.Scheme == Uri.UriSchemeHttps &&
                    (uri.Host.Equals(
                        "linkedin.com",
                        StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith(
                        ".linkedin.com",
                        StringComparison.OrdinalIgnoreCase));

                if (!validHost)
                {
                    ModelState.AddModelError(
                        nameof(model.LinkedInUrl),
                        "Enter a valid HTTPS LinkedIn URL.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FullName = user.FullName;
                ViewBag.ProfileCompletion =
                    CalculateCompletion(model);

                return View(model);
            }

            // IMPORTANT: User ID comes from login,
            // never from submitted form values.
            var profile = await _context.AlumniProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new AlumniProfile
                {
                    UserId = user.Id
                };

                _context.AlumniProfiles.Add(profile);
            }

            profile.Headline = model.Headline.Trim();
            profile.Industry = model.Industry.Trim();
            profile.JobTitle = Clean(model.JobTitle);
            profile.CompanyName = Clean(model.CompanyName);
            profile.About = Clean(model.About);
            profile.Skills = Clean(model.Skills);
            profile.LinkedInUrl = Clean(model.LinkedInUrl);
            profile.IsVisibleToStudents =
                model.IsVisibleToStudents;
            profile.IsAvailableForMentoring =
                model.IsAvailableForMentoring;
            profile.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your alumni profile has been saved.";

            return RedirectToAction(nameof(Edit));
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static int CalculateCompletion(
            AlumniProfileForm model)
        {
            string?[] fields =
            {
                model.Headline,
                model.Industry,
                model.JobTitle,
                model.CompanyName,
                model.About,
                model.Skills,
                model.LinkedInUrl
            };

            int completed = fields.Count(
                f => !string.IsNullOrWhiteSpace(f));

            return (int)Math.Round(
                completed * 100.0 / fields.Length);
        }
    }
}
