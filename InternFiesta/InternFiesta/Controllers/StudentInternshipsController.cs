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
        public async Task<IActionResult> Index()
        {
            var internships = await _context.InternshipPostings
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(internships);
        }
    }
}