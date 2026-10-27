
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using InternFiesta.Services;
using InternFiesta.ViewModels.Coordinator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class CoordinatorOverviewAdditionalTests
    {
        private static ApplicationDbContext CreateEmptyContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(
                        "CoordinatorT22_" +
                        Guid.NewGuid().ToString("N"))
                    .Options;

            return new ApplicationDbContext(options);
        }

        // TEST 1: Verify seeded representative counts.
        [Test]
        public async Task GetCountsAsync_WithSeededRecords_ReturnsCorrectDistinctCounts()
        {
            await using var db = CreateEmptyContext();

            db.Users.AddRange(
                new ApplicationUser
                {
                    Id = "student-1",
                    UserName = "student-1",
                    FullName = "Student One"
                },
                new ApplicationUser
                {
                    Id = "student-2",
                    UserName = "student-2",
                    FullName = "Student Two"
                },
                new ApplicationUser
                {
                    Id = "company-1",
                    UserName = "company-1",
                    FullName = "Test Company"
                },
                new ApplicationUser
                {
                    Id = "faculty-1",
                    UserName = "faculty-1",
                    FullName = "Test Faculty"
                },
                new ApplicationUser
                {
                    Id = "coordinator-1",
                    UserName = "coordinator-1",
                    FullName = "Test Coordinator"
                },
                new ApplicationUser
                {
                    Id = "no-role",
                    UserName = "no-role",
                    FullName = "Unassigned User"
                }
            );

            db.Roles.AddRange(
                new IdentityRole
                {
                    Id = "student-role",
                    Name = "Student",
                    NormalizedName = "STUDENT"
                },
                new IdentityRole
                {
                    Id = "company-role",
                    Name = "Company",
                    NormalizedName = "COMPANY"
                }
            );

            db.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    UserId = "student-1",
                    RoleId = "student-role"
                },
                new IdentityUserRole<string>
                {
                    UserId = "student-2",
                    RoleId = "student-role"
                },
                new IdentityUserRole<string>
                {
                    UserId = "company-1",
                    RoleId = "company-role"
                }
            );

            db.InternshipPostings.AddRange(
                new InternshipPosting
                {
                    Id = 101,
                    CompanyUserId = "company-1",
                    Title = "Software Intern",
                    Description = "Test internship",
                    Location = "Dhaka",
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Positions = 1,
                    IsDeleted = false
                },
                new InternshipPosting
                {
                    Id = 102,
                    CompanyUserId = "company-1",
                    Title = "Data Intern",
                    Description = "Another test internship",
                    Location = "Dhaka",
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Positions = 1,
                    IsDeleted = false
                },
                new InternshipPosting
                {
                    Id = 103,
                    CompanyUserId = "company-1",
                    Title = "Deleted Posting",
                    Description = "Should be filtered out",
                    Location = "Dhaka",
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Positions = 1,
                    IsDeleted = true
                }
            );

            db.InternshipApplications.AddRange(
                new InternshipApplication
                {
                    Id = 201,
                    StudentUserId = "student-1",
                    InternshipPostingId = 101,
                    Status = "Selected"
                },
                new InternshipApplication
                {
                    Id = 202,
                    StudentUserId = "student-2",
                    InternshipPostingId = 102,
                    Status = "Applied"
                },
                new InternshipApplication
                {
                    Id = 203,
                    StudentUserId = "student-1",
                    InternshipPostingId = 102,
                    Status = "Selected"
                }
            );

            // Two academic records for the SAME student.
            // Academic participant count should be 1.
            db.AcademicInternships.AddRange(
                new AcademicInternship
                {
                    Id = 301,
                    InternshipApplicationId = 201,
                    FacultyUserId = "faculty-1"
                },
                new AcademicInternship
                {
                    Id = 302,
                    InternshipApplicationId = 203,
                    FacultyUserId = "faculty-1"
                }
            );

            db.JobFairs.Add(
                new JobFair
                {
                    Id = 401,
                    Title = "Test Career Fair",
                    Description = "Local test event",
                    Venue = "UAP Campus",
                    StartsAtUtc = DateTime.UtcNow.AddDays(7),
                    EndsAtUtc = DateTime.UtcNow
                        .AddDays(7).AddHours(5),
                    CreatedByCoordinatorId = "coordinator-1"
                }
            );

            await db.SaveChangesAsync();

            var service = new CoordinatorOverviewService(db);
            var result = await service.GetCountsAsync();

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.StudentCount,
                    Is.EqualTo(2));

                Assert.That(
                    result.CompanyCount,
                    Is.EqualTo(1));

                Assert.That(
                    result.InternshipCount,
                    Is.EqualTo(2),
                    "Soft-deleted posting should be excluded.");

                Assert.That(
                    result.ApplicationCount,
                    Is.EqualTo(3));

                Assert.That(
                    result.JobFairCount,
                    Is.EqualTo(1));

                Assert.That(
                    result.AcademicParticipationCount,
                    Is.EqualTo(1),
                    "Two academic records belong to one student.");
            });
        }

        // TEST 2: Verify empty academic table page model.
        [Test]
        public async Task Index_WithNoAcademicRecords_ReturnsEmptyTableModel()
        {
            await using var db = CreateEmptyContext();

            var controller = new CoordinatorOverviewController(
                new CoordinatorOverviewService(db),
                db);

            var action = await controller.Index(
                CancellationToken.None);

            var view = action as ViewResult;

            Assert.That(view, Is.Not.Null);

            Assert.That(
                view!.Model,
                Is.TypeOf<CoordinatorOverviewPageViewModel>());

            var model =
                (CoordinatorOverviewPageViewModel)view.Model!;

            Assert.Multiple(() =>
            {
                Assert.That(
                    model.TotalAcademicRecords,
                    Is.Zero);

                Assert.That(
                    model.AcademicRows,
                    Is.Empty);
            });
        }

        // TEST 3: Check Controller authorization metadata.
        [Test]
        public void OverviewAndSummary_AreProtectedByCoordinatorOnlyAttribute()
        {
            var type = typeof(CoordinatorOverviewController);

            var authorize =
                type.GetCustomAttribute<AuthorizeAttribute>();

            Assert.That(authorize, Is.Not.Null);

            Assert.That(
                authorize!.Roles,
                Is.EqualTo("UniversityCoordinator"));

            Assert.That(
                type.GetCustomAttribute<AllowAnonymousAttribute>(),
                Is.Null);

            foreach (string actionName in new[]
            {
                "Index",
                "Summary"
            })
            {
                var method = type.GetMethod(actionName);

                Assert.That(method, Is.Not.Null);

                Assert.That(
                    method!.GetCustomAttribute<AllowAnonymousAttribute>(),
                    Is.Null);
            }
        }

        // TEST 4: Check ASP.NET Core's role requirement.
        [TestCase("UniversityCoordinator", true)]
        [TestCase("Student", false)]
        [TestCase("Company", false)]
        [TestCase("Alumni", false)]
        [TestCase("FacultySupervisor", false)]
        [TestCase("Administrator", false)]
        [TestCase(null, false)]
        public async Task CoordinatorRoleRequirement_AcceptsOnlyCoordinatorRole(
            string? userRole,
            bool expectedAccess)
        {
            var authorize =
                typeof(CoordinatorOverviewController)
                    .GetCustomAttribute<AuthorizeAttribute>()!;

            var allowedRoles = authorize.Roles!.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            var requirement =
                new RolesAuthorizationRequirement(allowedRoles);

            var claims = userRole == null
                ? Array.Empty<Claim>()
                : new[]
                {
                    new Claim(ClaimTypes.Role, userRole)
                };

            var identity = new ClaimsIdentity(
                claims,
                userRole == null ? null : "TestAuthentication");

            var principal = new ClaimsPrincipal(identity);

            var context = new AuthorizationHandlerContext(
                new IAuthorizationRequirement[]
                {
                    requirement
                },
                principal,
                resource: null);

            await requirement.HandleAsync(context);

            Assert.That(
                context.HasSucceeded,
                Is.EqualTo(expectedAccess),
                "Wrong authorization result for: " +
                (userRole ?? "anonymous"));
        }
    }
}

