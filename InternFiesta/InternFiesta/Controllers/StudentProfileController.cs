using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new StudentProfile
                {
                    UserId = userId
                };
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(StudentProfile model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            model.UserId = userId;

            ModelState.Remove(nameof(StudentProfile.UserId));
            ModelState.Remove(nameof(StudentProfile.User));

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var existingProfile = await _context.StudentProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile == null)
            {
                model.Id = 0;
                _context.StudentProfiles.Add(model);
            }
            else
            {
                existingProfile.StudentId = model.StudentId;
                existingProfile.Department = model.Department;
                existingProfile.CGPA = model.CGPA;
                existingProfile.Skills = model.Skills;
                existingProfile.Projects = model.Projects;
                existingProfile.Experience = model.Experience;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Profile saved successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}