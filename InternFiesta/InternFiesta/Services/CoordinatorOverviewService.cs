
using InternFiesta.Data;
using InternFiesta.ViewModels.Coordinator;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Services
{
    public class CoordinatorOverviewService
    {
        private readonly ApplicationDbContext _context;

        public CoordinatorOverviewService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CoordinatorOverviewCountsViewModel>
            GetCountsAsync(
                CancellationToken cancellationToken = default)
        {
            // Student accounts: count actual Identity role members.
            int studentCount = await (
                from userRole in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where role.Name == "Student"
                select userRole.UserId
            )
            .Distinct()
            .CountAsync(cancellationToken);

            // Company accounts: count actual Identity role members.
            int companyCount = await (
                from userRole in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where role.Name == "Company"
                select userRole.UserId
            )
            .Distinct()
            .CountAsync(cancellationToken);

            // Count non-deleted internship postings.
            // Existing global query filter excludes soft-deleted rows.
            int internshipCount = await _context.InternshipPostings
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Count all application records.
            int applicationCount = await _context.InternshipApplications
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Count all job fairs, including closed fairs.
            int jobFairCount = await _context.JobFairs
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Count distinct students participating
            // in the academic internship process.
            int academicParticipationCount = await (
                from academic in
                    _context.AcademicInternships.AsNoTracking()

                join application in
                    _context.InternshipApplications.AsNoTracking()

                    on academic.InternshipApplicationId
                    equals application.Id

                select application.StudentUserId
            )
            .Distinct()
            .CountAsync(cancellationToken);

            return new CoordinatorOverviewCountsViewModel
            {
                StudentCount = studentCount,
                CompanyCount = companyCount,
                InternshipCount = internshipCount,
                ApplicationCount = applicationCount,
                JobFairCount = jobFairCount,
                AcademicParticipationCount =
                    academicParticipationCount
            };
        }
    }
}
