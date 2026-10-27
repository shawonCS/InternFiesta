
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InternFiesta.Data;
using InternFiesta.Services;
using InternFiesta.ViewModels.Coordinator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Controllers
{
    [Authorize(Roles = "UniversityCoordinator")]
    [Route("Coordinator/Overview")]
    public class CoordinatorOverviewController : Controller
    {
        private readonly CoordinatorOverviewService _overviewService;
        private readonly ApplicationDbContext _context;

        public CoordinatorOverviewController(
            CoordinatorOverviewService overviewService,
            ApplicationDbContext context)
        {
            _overviewService = overviewService;
            _context = context;
        }

        // =========================================
        // T-22.2: Coordinator Dashboard UI
        // URL: /Coordinator/Overview
        // =========================================

        [HttpGet("")]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            // Reuse the verified T-22.1 counts.
            var counts = await _overviewService.GetCountsAsync(
                cancellationToken);

            // Count academic internship records.
            int totalAcademicRecords =
                await _context.AcademicInternships
                    .AsNoTracking()
                    .CountAsync(cancellationToken);

            // Retrieve up to 20 latest academic records.
            var academicRows =
                await _context.AcademicInternships
                    .AsNoTracking()
                    .OrderByDescending(a => a.StartedAt)
                    .ThenByDescending(a => a.Id)
                    .Take(20)
                    .Select(a => new CoordinatorAcademicRowViewModel
                    {
                        AcademicInternshipId = a.Id,

                        StudentName =
                            a.InternshipApplication.Student != null
                                ? a.InternshipApplication.Student.FullName
                                : "Unknown Student",

                        InternshipTitle =
                            a.InternshipApplication.InternshipPosting != null
                                ? a.InternshipApplication.InternshipPosting.Title
                                : "Unavailable Internship",

                        FacultyName =
                            a.FacultyUser != null
                                ? a.FacultyUser.FullName
                                : "Unassigned",

                        ApplicationStatus =
                            a.InternshipApplication.Status,

                        StartedAtUtc = a.StartedAt,

                        ProgressUpdateCount =
                            _context.AcademicProgressUpdates
                                .Count(p =>
                                    p.AcademicInternshipId == a.Id),

                        MilestoneCount =
                            _context.AcademicMilestones
                                .Count(m =>
                                    m.AcademicInternshipId == a.Id),

                        HasEvaluation =
                            _context.FacultyEvaluations
                                .Any(e =>
                                    e.AcademicInternshipId == a.Id)
                    })
                    .ToListAsync(cancellationToken);

            // Send counts and academic rows to the UI.
            var model = new CoordinatorOverviewPageViewModel
            {
                Counts = counts,
                TotalAcademicRecords = totalAcademicRecords,
                AcademicRows = academicRows
            };

            return View(model);
        }

        // =========================================
        // T-22.1: Nafsin's existing JSON endpoint
        // URL: /Coordinator/Overview/Summary
        // =========================================

        [HttpGet("Summary")]
        public async Task<IActionResult> Summary(
            CancellationToken cancellationToken)
        {
            // Prevent caching sensitive Coordinator data.
            Response.Headers.CacheControl = "no-store";

            var counts = await _overviewService.GetCountsAsync(
                cancellationToken);

            return Json(counts);
        }
    }
}
