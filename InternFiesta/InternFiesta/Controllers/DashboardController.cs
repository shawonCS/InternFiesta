using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternFiesta.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            if (User.IsInRole("Student"))
            {
                return View("Student");
            }

            if (User.IsInRole("Company"))
            {
                return View("Company");
            }

            if (User.IsInRole("FacultySupervisor"))
            {
                return View("Faculty");
            }

            if (User.IsInRole("UniversityCoordinator"))
            {
                return View("University");
            }

            if (User.IsInRole("Alumni"))
            {
                return View("Alumni");
            }

            if (User.IsInRole("Administrator"))
            {
                return View("Administrator");
            }

            return Forbid();
        }
    }
}