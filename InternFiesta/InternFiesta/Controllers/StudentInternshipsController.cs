using InternFiesta.Data;
using InternFiesta.Models;
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
            var studentUserId = _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

            if (internship == null)
            {
                return NotFound();
            }

            var application = new InternshipApplication
            {
                InternshipPostingId = internship.Id,
                StudentUserId = studentUserId,
                AppliedAt = DateTime.Now
            };

            _context.InternshipApplications.Add(application);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}