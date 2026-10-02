using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var companyUserId =
                _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var internship =
                await _context.InternshipPostings
                    .FirstOrDefaultAsync(i =>
                        i.Id == id &&
                        i.CompanyUserId == companyUserId);

            if (internship == null)
            {
                return NotFound();
            }

            var applications =
                await _context.InternshipApplications
                    .Where(a =>
                        a.InternshipPostingId == id)
                    .Include(a => a.Student)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

            var studentIds =
                applications
                    .Select(a => a.StudentUserId)
                    .Distinct()
                    .ToList();

            var profiles =
                await _context.StudentProfiles
                    .Where(p =>
                        studentIds.Contains(p.UserId))
                    .ToDictionaryAsync(p => p.UserId);

            var model =
                new CompanyApplicationsViewModel
                {
                    Internship = internship,

                    Applicants =
                        applications.Select(a =>
                            new CompanyApplicantViewModel
                            {
                                Application = a,
                                Student = a.Student,

                                Profile =
                                    profiles.GetValueOrDefault(
                                        a.StudentUserId)
                            })
                        .ToList()
                };

            return View(model);
        }
    }
}