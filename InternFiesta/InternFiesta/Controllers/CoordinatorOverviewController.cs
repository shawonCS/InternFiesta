
using InternFiesta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "UniversityCoordinator")]
    [Route("Coordinator/Overview")]
    public class CoordinatorOverviewController : Controller
    {
        private readonly CoordinatorOverviewService _overviewService;

        public CoordinatorOverviewController(
            CoordinatorOverviewService overviewService)
        {
            _overviewService = overviewService;
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> Summary(
            CancellationToken cancellationToken)
        {
            // Prevent caching sensitive coordinator data.
            Response.Headers.CacheControl = "no-store";

            var counts = await _overviewService.GetCountsAsync(
                cancellationToken);

            return Json(counts);
        }
    }
}
