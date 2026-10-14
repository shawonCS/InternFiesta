
using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class AcademicInternshipTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static AcademicProgressController CreateController(ApplicationDbContext db, string userId)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            var manager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);

            manager
                .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(userId);

            var controller = new AcademicProgressController(
                db, manager.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            controller.TempData = new TempDataDictionary(
                controller.HttpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        private static async Task SeedAsync(ApplicationDbContext db)
        {
            db.Users.AddRange(
                new ApplicationUser
                {
                    Id = "student-1",
                    UserName = "student-1"
                },
                new ApplicationUser
                {
                    Id = "student-2",
                    UserName = "student-2"
                },
                new ApplicationUser
                {
                    Id = "company-1",
                    UserName = "company-1"
                },
                new ApplicationUser
                {
                    Id = "company-2",
                    UserName = "company-2"
                },
                new ApplicationUser
                {
                    Id = "faculty-1",
                    UserName = "faculty-1"
                },
                new ApplicationUser
                {
                    Id = "faculty-2",
                    UserName = "faculty-2"
                }
            );

            db.InternshipPostings.Add(new InternshipPosting
            {
                Id = 1,
                CompanyUserId = "company-1",
                Title = "Software Intern",
                Description = "Test posting",
                Location = "Dhaka",
                Deadline = DateTime.UtcNow.AddDays(30),
                Positions = 1
            });

            db.InternshipApplications.Add(new InternshipApplication
            {
                Id = 1,
                InternshipPostingId = 1,
                StudentUserId = "student-1",
                Status = "Selected",
                AppliedAt = DateTime.UtcNow
            });

            db.AcademicInternships.Add(new AcademicInternship
            {
                Id = 1,
                InternshipApplicationId = 1,
                FacultyUserId = "faculty-1",
                StartedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        [Test]
        public async Task SubmitProgress_Owner_SavesTrimmedProgress()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "student-1");

            var result = await controller.SubmitProgress(
                1,
                new AcademicProgressController.ProgressForm
                {
                    Description = "  Finished milestone  "
                });

            Assert.That(result,
                Is.TypeOf<RedirectToActionResult>());

            Assert.That(
                await db.AcademicProgressUpdates.CountAsync(),
                Is.EqualTo(1));

            var progress =
                await db.AcademicProgressUpdates.SingleAsync();

            Assert.That(progress.Description,
                Is.EqualTo("Finished milestone"));
        }

        [Test]
        public async Task SubmitProgress_OtherStudent_ReturnsNotFound()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "student-2");

            var result = await controller.SubmitProgress(
                1,
                new AcademicProgressController.ProgressForm
                {
                    Description = "Unauthorized"
                });

            Assert.That(result, Is.TypeOf<NotFoundResult>());

            Assert.That(
                await db.AcademicProgressUpdates.CountAsync(),
                Is.Zero);
        }

        [Test]
        public async Task SubmitProgress_InvalidModel_DoesNotSave()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "student-1");

            controller.ModelState.AddModelError(
                "Description", "Required");

            var result = await controller.SubmitProgress(
                1,
                new AcademicProgressController.ProgressForm
                {
                    Description = ""
                });

            Assert.That(result, Is.TypeOf<ViewResult>());

            Assert.That(
                await db.AcademicProgressUpdates.CountAsync(),
                Is.Zero);
        }

        [Test]
        public async Task SubmitFeedback_OwningCompany_SavesFeedback()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "company-1");

            var result = await controller.SubmitFeedback(
                1,
                new AcademicProgressController.FeedbackForm
                {
                    Rating = 4,
                    Comments = "  Good work  "
                });

            Assert.That(result,
                Is.TypeOf<RedirectToActionResult>());

            var feedback =
                await db.AcademicCompanyFeedbacks.SingleAsync();

            Assert.That(feedback.Rating, Is.EqualTo(4));
            Assert.That(feedback.Comments,
                Is.EqualTo("Good work"));
            Assert.That(feedback.CompanyUserId,
                Is.EqualTo("company-1"));
        }

        [Test]
        public async Task SubmitFeedback_OtherCompany_ReturnsNotFound()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "company-2");

            var result = await controller.SubmitFeedback(
                1,
                new AcademicProgressController.FeedbackForm
                {
                    Rating = 5,
                    Comments = "Wrong company"
                });

            Assert.That(result, Is.TypeOf<NotFoundResult>());

            Assert.That(
                await db.AcademicCompanyFeedbacks.CountAsync(),
                Is.Zero);
        }

        [Test]
        public async Task FacultyStudents_OnlyReturnsOwnAssignments()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-2");

            var result = await controller.FacultyStudents();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var view = (ViewResult)result;

            var internships =
                (System.Collections.Generic.List<AcademicInternship>)
                view.Model!;

            Assert.That(internships, Is.Empty);
        }

        [Test]
        public async Task FacultyStudentDetails_OtherFaculty_ReturnsNotFound()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-2");

            var result =
                await controller.FacultyStudentDetails(1);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task FacultyStudentDetails_AssignedFaculty_ReturnsView()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-1");

            var result =
                await controller.FacultyStudentDetails(1);

            Assert.That(result, Is.TypeOf<ViewResult>());

            var internship =
                ((ViewResult)result).Model as AcademicInternship;

            Assert.That(internship, Is.Not.Null);

            Assert.That(internship!.FacultyUserId,
                Is.EqualTo("faculty-1"));
        }
    }
}
