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
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class FacultyEvaluationTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            return new ApplicationDbContext(options);
        }

        private static FacultyEvaluationsController CreateController(
            ApplicationDbContext db,
            string facultyId)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            var manager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);

            manager
                .Setup(m => m.GetUserId(
                    It.IsAny<ClaimsPrincipal>()))
                .Returns(facultyId);

            var controller = new FacultyEvaluationsController(
                db,
                manager.Object);

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
                    UserName = "student-1",
                    FullName = "Test Student",
                    Email = "student@test.com"
                },
                new ApplicationUser
                {
                    Id = "faculty-1",
                    UserName = "faculty-1",
                    FullName = "Assigned Faculty"
                },
                new ApplicationUser
                {
                    Id = "faculty-2",
                    UserName = "faculty-2",
                    FullName = "Other Faculty"
                },
                new ApplicationUser
                {
                    Id = "company-1",
                    UserName = "company-1",
                    FullName = "Test Company"
                }
            );

            db.InternshipPostings.Add(new InternshipPosting
            {
                Id = 1,
                CompanyUserId = "company-1",
                Title = "Backend Developer Intern",
                Description = "Backend development internship",
                RequiredSkills = "C#, SQL Server",
                Location = "Dhaka",
                Positions = 2,
                Deadline = DateTime.UtcNow.AddDays(30),
                IsActive = true
            });

            db.InternshipApplications.Add(
                new InternshipApplication
                {
                    Id = 1,
                    InternshipPostingId = 1,
                    StudentUserId = "student-1",
                    Status = "Selected",
                    AppliedAt = DateTime.UtcNow
                });

            db.AcademicInternships.Add(
                new AcademicInternship
                {
                    Id = 1,
                    InternshipApplicationId = 1,
                    FacultyUserId = "faculty-1",
                    StartedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync();
        }

        private static FacultyEvaluationsController.EvaluationForm
            ValidForm(string grade = "A")
        {
            return new FacultyEvaluationsController.EvaluationForm
            {
                Grade = grade,
                Comments =
                    "The student completed the assigned internship tasks " +
                    "and demonstrated good technical skills."
            };
        }

        [Test]
        public async Task AssignedFaculty_CanSaveEvaluation()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-1");

            var result = await controller.Edit(1, ValidForm());

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var evaluation =
                await db.FacultyEvaluations.SingleAsync();

            Assert.That(evaluation.Grade, Is.EqualTo("A"));

            Assert.That(
                evaluation.FacultyUserId,
                Is.EqualTo("faculty-1"));

            Assert.That(
                evaluation.AcademicInternshipId,
                Is.EqualTo(1));

            Assert.That(
                evaluation.Comments,
                Does.Contain("technical skills"));
        }

        [Test]
        public async Task OtherFaculty_CannotSaveEvaluation()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-2");

            var result = await controller.Edit(1, ValidForm());

            Assert.That(result, Is.TypeOf<NotFoundResult>());

            Assert.That(
                await db.FacultyEvaluations.CountAsync(),
                Is.Zero);
        }

        [Test]
        public async Task OtherFaculty_CannotOpenEvaluationForm()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-2");

            var result = await controller.Edit(1);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task UpdatingEvaluation_DoesNotCreateDuplicate()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-1");

            await controller.Edit(1, ValidForm("B"));

            var result = await controller.Edit(1, ValidForm("A+"));

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            Assert.That(
                await db.FacultyEvaluations.CountAsync(),
                Is.EqualTo(1));

            var evaluation =
                await db.FacultyEvaluations.SingleAsync();

            Assert.That(evaluation.Grade, Is.EqualTo("A+"));
        }

        [Test]
        public async Task InvalidGrade_IsNotSaved()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-1");

            var result = await controller.Edit(1, ValidForm("Z"));

            Assert.That(result, Is.TypeOf<ViewResult>());

            Assert.That(
                await db.FacultyEvaluations.CountAsync(),
                Is.Zero);

            Assert.That(controller.ModelState.IsValid, Is.False);
        }

        [Test]
        public async Task SavedEvaluation_OwnerCanViewSummary()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-1");

            await controller.Edit(1, ValidForm("A"));

            var result = await controller.Summary(1);

            Assert.That(result, Is.TypeOf<ViewResult>());

            var evaluation =
                ((ViewResult)result).Model as FacultyEvaluation;

            Assert.That(evaluation, Is.Not.Null);

            Assert.That(evaluation!.Grade, Is.EqualTo("A"));
        }

        [Test]
        public async Task SavedEvaluation_OtherFacultyCannotViewSummary()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var owner = CreateController(db, "faculty-1");

            await owner.Edit(1, ValidForm("A"));

            var otherFaculty = CreateController(db, "faculty-2");

            var result = await otherFaculty.Summary(1);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task FirstEvaluation_NotifiesStudent()
        {
            using var db = CreateContext();
            await SeedAsync(db);

            var controller = CreateController(db, "faculty-1");

            await controller.Edit(1, ValidForm("A"));

            var notification = await db.Notifications
                .SingleAsync();

            Assert.That(
                notification.UserId,
                Is.EqualTo("student-1"));

            Assert.That(
                notification.Title,
                Is.EqualTo("Faculty Evaluation Completed"));
        }
    }
}