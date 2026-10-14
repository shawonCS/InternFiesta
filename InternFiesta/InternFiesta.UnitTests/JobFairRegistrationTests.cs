using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class JobFairRegistrationTests
    {
        // Create a separate temporary database for each test.
        private static ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            return new ApplicationDbContext(options);
        }

        // Create controller with a logged-in Student.
        private static JobFairsController CreateController(
            ApplicationDbContext db,
            string userId = "student-1",
            string role = "Student")
        {
            var controller = new JobFairsController(db);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(
                claims, "TestAuthentication");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            var provider = new Mock<ITempDataProvider>();

            provider.Setup(p =>
                p.LoadTempData(It.IsAny<HttpContext>()))
                .Returns(new Dictionary<string, object>());

            controller.TempData = new TempDataDictionary(
                controller.HttpContext,
                provider.Object);

            return controller;
        }

        // Add a sample Job Fair.
        private static async Task<JobFair> AddJobFair(
            ApplicationDbContext db,
            bool isClosed = false,
            bool hasEnded = false)
        {
            var start = hasEnded
                ? DateTime.UtcNow.AddDays(-3)
                : DateTime.UtcNow.AddDays(5);

            var fair = new JobFair
            {
                Title = "Unit Test Career Fair",
                Description = "A job fair for testing registration.",
                Venue = "UAP Auditorium",
                StartsAtUtc = start,
                EndsAtUtc = start.AddHours(5),
                IsClosed = isClosed,
                CreatedByCoordinatorId = "coordinator-1"
            };

            db.JobFairs.Add(fair);
            await db.SaveChangesAsync();

            return fair;
        }

        [Test]
        public async Task Student_CanRegisterForOpenJobFair()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db);
            var controller = CreateController(db);

            var result = await controller.Register(fair.Id);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var registration =
                await db.JobFairRegistrations.SingleAsync();

            Assert.That(
                registration.UserId,
                Is.EqualTo("student-1"));

            Assert.That(
                registration.JobFairId,
                Is.EqualTo(fair.Id));

            Assert.That(
                registration.ParticipantRole,
                Is.EqualTo("Student"));
        }

        [Test]
        public async Task DuplicateRegistration_IsBlocked()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db);
            var controller = CreateController(db);

            // First registration
            await controller.Register(fair.Id);

            // Same student registers again
            var secondResult =
                await controller.Register(fair.Id);

            Assert.That(
                secondResult,
                Is.TypeOf<RedirectToActionResult>());

            // Only one registration should exist
            Assert.That(
                await db.JobFairRegistrations.CountAsync(),
                Is.EqualTo(1));

            Assert.That(
                controller.TempData["WarningMessage"],
                Does.Contain("already registered"));
        }

        [Test]
        public async Task ClosedJobFair_RejectsRegistration()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(
                db, isClosed: true);

            var controller = CreateController(db);

            var result = await controller.Register(fair.Id);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            Assert.That(
                await db.JobFairRegistrations.CountAsync(),
                Is.Zero);

            Assert.That(
                controller.TempData["ErrorMessage"],
                Does.Contain("closed"));
        }

        [Test]
        public async Task EndedJobFair_RejectsRegistration()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(
                db, hasEnded: true);

            var controller = CreateController(db);

            await controller.Register(fair.Id);

            Assert.That(
                await db.JobFairRegistrations.CountAsync(),
                Is.Zero);
        }

        [Test]
        public async Task DifferentStudents_CanRegisterForSameFair()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db);

            var studentOne = CreateController(
                db, "student-1");

            var studentTwo = CreateController(
                db, "student-2");

            await studentOne.Register(fair.Id);
            await studentTwo.Register(fair.Id);

            Assert.That(
                await db.JobFairRegistrations.CountAsync(),
                Is.EqualTo(2));
        }

        [Test]
        public async Task Company_CanRegisterForOpenFair()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db);

            var company = CreateController(
                db, "company-1", "Company");

            await company.Register(fair.Id);

            var registration =
                await db.JobFairRegistrations.SingleAsync();

            Assert.That(
                registration.ParticipantRole,
                Is.EqualTo("Company"));

            Assert.That(
                registration.UserId,
                Is.EqualTo("company-1"));
        }

        [Test]
        public async Task MissingJobFair_ReturnsNotFound()
        {
            using var db = CreateContext();

            var controller = CreateController(db);

            var result = await controller.Register(999);

            Assert.That(
                result,
                Is.TypeOf<NotFoundResult>());

            Assert.That(
                await db.JobFairRegistrations.CountAsync(),
                Is.Zero);
        }
    }
}