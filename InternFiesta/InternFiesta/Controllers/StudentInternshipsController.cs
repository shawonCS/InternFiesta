using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
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
            ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search,string? location,string? skills)
        {
            search = search?.Trim();
            location = location?.Trim();
            skills = skills?.Trim();
            var query = _context.InternshipPostings
                .Where(i => i.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Title.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(i =>
                    i.Location != null &&
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

            var internship =
                await _context.InternshipPostings
                    .FirstOrDefaultAsync(i =>
                        i.Id == id &&
                        i.IsActive);

            if (internship == null)
            {
                return NotFound();
            }

            // Check profile completeness before allowing application
            var profile =
                await _context.StudentProfiles
                    .FirstOrDefaultAsync(p =>
                        p.UserId == studentUserId);

            var completeness =
                ProfileCompletenessService.Calculate(
                    profile);

            if (!completeness.CanApply)
            {
                TempData["ProfileIncompleteError"] =
                    "Please complete the required profile information before applying. Missing: "
                    + string.Join(
                        ", ",
                        completeness.MissingRequiredFields)
                    + ".";

                return RedirectToAction(
                    nameof(Index));
            }

            // Prevent duplicate application
            var alreadyApplied =
                await _context.InternshipApplications
                    .AnyAsync(a =>
                        a.InternshipPostingId == id &&
                        a.StudentUserId == studentUserId);

            if (alreadyApplied)
            {
                TempData["ApplicationError"] =
                    "You have already applied for this internship.";

                return RedirectToAction(
                    nameof(Index));
            }

            var application =
                new InternshipApplication
                {
                    InternshipPostingId = id,
                    StudentUserId = studentUserId,
                    Status = "Applied",
                    AppliedAt = DateTime.Now
                };

            _context.InternshipApplications.Add(
                application);

            await _context.SaveChangesAsync();
            TempData["ApplicationSuccess"] =
            "Your application was submitted successfully.";

            return RedirectToAction(
                nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id)
        {
            var studentUserId = _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.IsActive);

            if (internship == null)
            {
                return NotFound();
            }

            var alreadySaved = await _context.SavedInternships
                .AnyAsync(s =>
                    s.InternshipPostingId == id &&
                    s.StudentUserId == studentUserId);

            if (!alreadySaved)
            {
                var savedInternship = new SavedInternship
                {
                    InternshipPostingId = id,
                    StudentUserId = studentUserId,
                    SavedAt = DateTime.Now
                };

                _context.SavedInternships.Add(savedInternship);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Saved()
        {
            var studentUserId = _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var savedInternships = await _context.SavedInternships
                .Where(s => s.StudentUserId == studentUserId)
                .Include(s => s.InternshipPosting)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            return View(savedInternships);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSaved(int id)
        {
            var studentUserId = _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var savedInternship = await _context.SavedInternships
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.StudentUserId == studentUserId);

            if (savedInternship == null)
            {
                return NotFound();
            }

            _context.SavedInternships.Remove(savedInternship);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Saved));
        }
        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var studentUserId = _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var applications = await _context.InternshipApplications
                .Where(a => a.StudentUserId == studentUserId)
                .Include(a => a.InternshipPosting)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }
    }
}