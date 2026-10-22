using InternFiesta.Models;
using InternFiesta.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InternFiesta.Data;
using Microsoft.EntityFrameworkCore;
namespace InternFiesta.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        
        // REGISTER
        
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }



        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            string[] allowedRoles =
            {
        "Student", "Company", "Alumni"
    };

            if (!allowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Please select a valid account type.");
            }

            // LinkedIn is optional, but must be valid if provided.
            if (model.Role == "Alumni" &&
                !string.IsNullOrWhiteSpace(model.AlumniLinkedInUrl) &&
                !IsSecureLinkedInUrl(model.AlumniLinkedInUrl))
            {
                ModelState.AddModelError(
                    nameof(model.AlumniLinkedInUrl),
                    "Enter a valid HTTPS LinkedIn URL.");
            }

            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim();

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                UserName = email,
                Email = email
            };

            // Account + role + profile are one transaction.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var createResult =
                await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return View(model);
            }

            var roleResult =
                await _userManager.AddToRoleAsync(user, model.Role);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return View(model);
            }

            if (model.Role == "Alumni")
            {
                var profile = new AlumniProfile
                {
                    UserId = user.Id,

                    // Legacy database column kept for compatibility.
                    Headline = string.Empty,

                    Industry = CleanRequiredColumn(
                        model.AlumniIndustry),

                    JobTitle = Clean(model.AlumniJobTitle),
                    CompanyName = Clean(model.AlumniCompanyName),
                    About = Clean(model.AlumniAbout),
                    Skills = Clean(model.AlumniSkills),
                    LinkedInUrl = Clean(model.AlumniLinkedInUrl),

                    // Alumni controls this later.
                    IsVisibleToStudents = false,
                    IsAvailableForMentoring = false,

                    UpdatedAtUtc = DateTime.UtcNow
                };

                _context.AlumniProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await _signInManager.SignInAsync(
                user, isPersistent: false);

            if (model.Role == "Alumni")
            {
                return RedirectToAction(
                    "Index", "AlumniProfiles");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        private static string CleanRequiredColumn(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool IsSecureLinkedInUrl(string value)
        {
            if (!Uri.TryCreate(
                value.Trim(), UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttps &&
                (uri.Host.Equals(
                    "linkedin.com",
                    StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(
                    ".linkedin.com",
                    StringComparison.OrdinalIgnoreCase));
        }



        // LOGIN

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe ?? false,
                    lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        
        // LOGOUT

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        
        // ACCESS DENIED
        

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}