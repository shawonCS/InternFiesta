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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
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

            if (internship.IsActive)
            {
                TempData["SuccessMessage"] =
                    "This internship is already active.";

                return RedirectToAction(nameof(Index));
            }

            internship.IsActive = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Internship reactivated successfully.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
                return Challenge();

            var internship = await _context.InternshipPostings
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.CompanyUserId == companyUserId);

            if (internship == null)
                return NotFound();

            if (internship.IsActive)
            {
                TempData["WarningMessage"] =
                    "Please close this internship before deleting it.";

                return RedirectToAction(nameof(Index));
            }

            bool hasApplications = await _context.InternshipApplications
                .AnyAsync(a => a.InternshipPostingId == id);

            if (hasApplications)
            {
                TempData["WarningMessage"] =
                    "This internship cannot be deleted because students have already applied.";

                return RedirectToAction(nameof(Index));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var savedInternships = await _context.SavedInternships
                .Where(s => s.InternshipPostingId == id)
                .ToListAsync();

            _context.SavedInternships.RemoveRange(savedInternships);
            _context.InternshipPostings.Remove(internship);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Internship posting deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> ids)
        {
            var companyUserId = _userManager.GetUserId(User);

            if (companyUserId == null)
                return Challenge();

            if (ids == null || ids.Count == 0)
            {
                TempData["WarningMessage"] =
                    "Please select at least one internship.";

                return RedirectToAction(nameof(Index));
            }

            var uniqueIds = ids.Distinct().ToList();

            var internships = await _context.InternshipPostings
                .Where(i =>
                    uniqueIds.Contains(i.Id) &&
                    i.CompanyUserId == companyUserId)
                .ToListAsync();

            if (internships.Count != uniqueIds.Count)
            {
                TempData["ErrorMessage"] =
                    "One or more selected internships were not found or do not belong to your company.";

                return RedirectToAction(nameof(Index));
            }

            if (internships.Any(i => i.IsActive))
            {
                TempData["WarningMessage"] =
                    "Close all selected internships before deleting them.";

                return RedirectToAction(nameof(Index));
            }

            bool hasApplications = await _context.InternshipApplications
                .AnyAsync(a => uniqueIds.Contains(a.InternshipPostingId));

            if (hasApplications)
            {
                TempData["WarningMessage"] =
                    "Some selected internships have student applications. No postings were deleted.";

                return RedirectToAction(nameof(Index));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var savedInternships = await _context.SavedInternships
                .Where(s => uniqueIds.Contains(s.InternshipPostingId))
                .ToListAsync();

            _context.SavedInternships.RemoveRange(savedInternships);
            _context.InternshipPostings.RemoveRange(internships);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"{internships.Count} internship posting(s) deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}