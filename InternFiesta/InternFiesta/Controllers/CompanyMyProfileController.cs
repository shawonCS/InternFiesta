
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyMyProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyMyProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class CompanyMyProfileViewModel
        {
            public string? UserId { get; set; }

            public string? CompanyName { get; set; }

            public string? Email { get; set; }

            public string? ProfilePhotoFileName { get; set; }

            [StringLength(1500)]
            public string About { get; set; } = "";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var profile = await _context.CompanyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var model = new CompanyMyProfileViewModel
            {
                UserId = user.Id,
                CompanyName = profile?.CompanyName ?? user.FullName,
                Email = user.Email,
                ProfilePhotoFileName = user.ProfilePhotoFileName,
                About = profile?.About ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAbout(
            [FromForm] string? about)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var description = about?.Trim() ?? "";

            if (description.Length > 1500)
            {
                TempData["ErrorMessage"] =
                    "About Company cannot exceed 1500 characters.";

                return RedirectToAction(nameof(Index));
            }

            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new CompanyProfile
                {
                    UserId = user.Id,
                    CompanyName = user.FullName,
                    About = description,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _context.CompanyProfiles.Add(profile);
            }
            else
            {
                profile.About = description;
                profile.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Company profile updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
