using InternFiesta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentInternshipsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentInternshipsController(
            ApplicationDbContext context)
        {
            _context = context;
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
    }
}