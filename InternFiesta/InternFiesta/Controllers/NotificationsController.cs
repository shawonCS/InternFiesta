using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly
            UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId =
                _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var notifications =
                await _context.Notifications
                    .Where(n =>
                        n.UserId == userId)
                    .OrderByDescending(n =>
                        n.CreatedAt)
                    .ToListAsync();

            return View(notifications);
        }
    }
}