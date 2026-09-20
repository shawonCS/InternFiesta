using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "Company")]
    public class InternshipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InternshipsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Show only internships created by the logged-in company
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var internships = await _context.InternshipPostings
                .Where(i => i.CompanyUserId == companyUserId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(internships);
        }


        // Open create internship page
        [HttpGet]
        public IActionResult Create()
        {
            var model = new InternshipPosting
            {
                Deadline = DateTime.Today.AddDays(30),
                Positions = 1
            };

            return View(model);
        }


        // Save new internship
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            InternshipPosting model)
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            model.CompanyUserId = companyUserId;
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;

            ModelState.Remove(nameof(InternshipPosting.CompanyUserId));
            ModelState.Remove(nameof(InternshipPosting.CompanyUser));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.InternshipPostings.Add(model);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Internship posting created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // Open edit page
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.CompanyUserId == companyUserId);

            if (internship == null)
            {
                return NotFound();
            }

            return View(internship);
        }


        // Save edited internship
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            InternshipPosting model)
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.CompanyUserId == companyUserId);

            if (internship == null)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(InternshipPosting.CompanyUserId));
            ModelState.Remove(nameof(InternshipPosting.CompanyUser));

            if (!ModelState.IsValid)
            {
                model.Id = id;
                return View(model);
            }

            internship.Title = model.Title;
            internship.Description = model.Description;
            internship.RequiredSkills = model.RequiredSkills;
            internship.Location = model.Location;
            internship.Deadline = model.Deadline;
            internship.Positions = model.Positions;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Internship posting updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // Close internship instead of deleting it
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
            {
                return Challenge();
            }

            var internship = await _context.InternshipPostings
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.CompanyUserId == companyUserId);

            if (internship == null)
            {
                return NotFound();
            }

            internship.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Internship posting closed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}