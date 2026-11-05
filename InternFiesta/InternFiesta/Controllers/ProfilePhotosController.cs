using InternFiesta.Models;
using InternFiesta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    // One photo workflow for ALL roles. No separate Student/Faculty/etc upload endpoints.
    [Authorize]
    public class ProfilePhotosController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly UploadFileStorage _storage;

        public ProfilePhotosController(
            UserManager<ApplicationUser> users,
            UploadFileStorage storage)
        {
            _users = users;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(8_000_000)]
        public async Task<IActionResult> Upload(IFormFile? photo,
            CancellationToken cancellationToken)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            if (photo == null)
            {
                TempData["ErrorMessage"] = "Choose a JPG or PNG image.";
                return await ReturnToProfile(user);
            }

            string newName;
            try
            {
                newName = await _storage.SaveAvatarAsync(photo, cancellationToken);
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return await ReturnToProfile(user);
            }

            string? oldName = user.ProfilePhotoFileName;
            user.ProfilePhotoFileName = newName;
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _storage.DeleteAvatar(newName);
                TempData["ErrorMessage"] = "Could not save your profile picture.";
                return await ReturnToProfile(user);
            }

            _storage.DeleteAvatar(oldName);
            TempData["SuccessMessage"] = "Profile picture updated.";
            return await ReturnToProfile(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            var oldName = user.ProfilePhotoFileName;
            user.ProfilePhotoFileName = null;
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Could not remove your profile picture.";
                return await ReturnToProfile(user);
            }

            _storage.DeleteAvatar(oldName);
            TempData["SuccessMessage"] = "Profile picture removed.";
            return await ReturnToProfile(user);
        }

        // Other signed-in users can see profile pictures; nobody can use this
        // endpoint to change another user's picture.
        [HttpGet]
        public async Task<IActionResult> Photo(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var name = await _users.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => u.ProfilePhotoFileName)
                .FirstOrDefaultAsync();

            var path = _storage.AvatarPath(name);
            if (path == null || !System.IO.File.Exists(path))
                return NotFound();

            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(path, "image/jpeg");
        }
        private async Task<IActionResult> ReturnToProfile(ApplicationUser user)
        {
            if (await _users.IsInRoleAsync(user, "Student"))
            {
                return RedirectToAction("Index", "StudentProfile");
            }

            if (await _users.IsInRoleAsync(user, "Company"))
            {
                return RedirectToAction("Index", "CompanyMyProfile");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
