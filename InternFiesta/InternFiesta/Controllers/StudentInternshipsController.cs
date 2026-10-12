
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentInternshipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentInternshipsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================
        // Browse Internship Opportunities
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? location,
            string? skills)
        {
            search = search?.Trim();
            location = location?.Trim();
            skills = skills?.Trim();

            var query = _context.InternshipPostings
                .AsNoTracking()
                .Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Title.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(i =>
                    i.Location.Contains(location));
            }

            if (!string.IsNullOrWhiteSpace(skills))
            {
                query = query.Where(i =>
                    i.RequiredSkills != null &&
                    i.RequiredSkills.Contains(skills));
            }

            var internships = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(internships);
        }

        // ============================================
        // Apply for an Internship
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id)
        {
            var studentUserId =
                _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.IsActive);

            if (internship == null)
            {
                TempData["WarningMessage"] =
                    "This internship is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            // Check application deadline
            if (internship.Deadline.Date < DateTime.UtcNow.Date)
            {
                TempData["WarningMessage"] =
                    "The application deadline for this internship has passed.";

                return RedirectToAction(nameof(Index));
            }

            // Prevent duplicate applications
            var alreadyApplied =
                await _context.InternshipApplications
                    .AnyAsync(a =>
                        a.InternshipPostingId == id &&
                        a.StudentUserId == studentUserId);

            if (alreadyApplied)
            {
                TempData["WarningMessage"] =
                    "You have already applied for this internship.";

                return RedirectToAction(nameof(Index));
            }

            // Check student profile completeness
            var profile = await _context.StudentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.UserId == studentUserId);

            var completeness =
                ProfileCompletenessService.Calculate(profile);

            if (!completeness.CanApply)
            {
                TempData["WarningMessage"] =
                    "Please complete your profile before applying. Missing: "
                    + string.Join(
                        ", ",
                        completeness.MissingRequiredFields)
                    + ".";

                return RedirectToAction(nameof(Index));
            }

            // Create internship application
            var application = new InternshipApplication
            {
                InternshipPostingId = id,
                StudentUserId = studentUserId,
                Status = "Applied",
                AppliedAt = DateTime.UtcNow
            };

            _context.InternshipApplications.Add(application);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your application was submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ============================================
        // Save Internship
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id)
        {
            var studentUserId =
                _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.IsActive);

            if (internship == null)
            {
                TempData["WarningMessage"] =
                    "This internship is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            var alreadySaved =
                await _context.SavedInternships
                    .AnyAsync(s =>
                        s.InternshipPostingId == id &&
                        s.StudentUserId == studentUserId);

            if (alreadySaved)
            {
                TempData["WarningMessage"] =
                    "You have already saved this internship.";

                return RedirectToAction(nameof(Index));
            }

            var savedInternship = new SavedInternship
            {
                InternshipPostingId = id,
                StudentUserId = studentUserId,
                SavedAt = DateTime.UtcNow
            };

            _context.SavedInternships.Add(savedInternship);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Internship saved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ============================================
        // View Saved Internships
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Saved()
        {
            var studentUserId =
                _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var savedInternships =
                await _context.SavedInternships
                    .AsNoTracking()
                    .Where(s =>
                        s.StudentUserId == studentUserId)
                    .Include(s => s.InternshipPosting)
                    .OrderByDescending(s => s.SavedAt)
                    .ToListAsync();

            return View(savedInternships);
        }

        // ============================================
        // Remove Saved Internship
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSaved(int id)
        {
            var studentUserId =
                _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var savedInternship =
                await _context.SavedInternships
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.StudentUserId == studentUserId);

            if (savedInternship == null)
            {
                TempData["WarningMessage"] =
                    "This saved internship could not be found.";

                return RedirectToAction(nameof(Saved));
            }

            _context.SavedInternships.Remove(savedInternship);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Internship removed from your saved list.";

            return RedirectToAction(nameof(Saved));
        }

        // ============================================
        // View My Applications
        // ============================================

        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var studentUserId =
                _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var applications =
                await _context.InternshipApplications
                    .AsNoTracking()
                    .Where(a =>
                        a.StudentUserId == studentUserId)
                    .Include(a => a.InternshipPosting)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

            return View(applications);
        }
    }
}
