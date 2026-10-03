using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentInterviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentInterviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var studentUserId =
                _userManager.GetUserId(User);

            if (studentUserId == null)
            {
                return Challenge();
            }

            var interviews =
                await _context.InterviewSchedules
                    .Include(i => i.InternshipApplication)
                    .ThenInclude(a => a!.InternshipPosting)
                    .Where(i =>
                        i.InternshipApplication != null &&
                        i.InternshipApplication.StudentUserId ==
                            studentUserId)
                    .OrderBy(i => i.InterviewDateTime)
                    .ToListAsync();

            return View(interviews);
        }
    }
}