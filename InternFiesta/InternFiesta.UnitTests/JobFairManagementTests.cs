
using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class JobFairManagementTests
    {
        // Each test gets a separate temporary database.
        private static ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            return new ApplicationDbContext(options);
        }

        // Create the controller without running the website.
        private static CoordinatorJobFairsController CreateController(
            ApplicationDbContext db)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            var manager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);

            manager.Setup(m =>
                m.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("coordinator-1");

            var controller = new CoordinatorJobFairsController(
                db, manager.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var tempDataProvider = new Mock<ITempDataProvider>();

            tempDataProvider
                .Setup(p => p.LoadTempData(
                    It.IsAny<HttpContext>()))
                .Returns(new Dictionary<string, object>());

            controller.TempData = new TempDataDictionary(
                controller.HttpContext,
                tempDataProvider.Object);

            return controller;
        }

        // Valid job fair form.
        private static CoordinatorJobFairsController.JobFairForm
            ValidForm()
        {
            var start = DateTime.UtcNow
                .AddHours(6)
                .AddDays(10);

            return new CoordinatorJobFairsController.JobFairForm
            {
                Title = "UAP Career Fair",
                Description =
                    "Career and internship opportunities for students.",
                Venue = "UAP Campus, Dhaka",
                StartsAt = start,
                EndsAt = start.AddHours(5)
            };
        }

        // Create a sample job fair for testing.
        private static async Task<JobFair> AddJobFair(
            ApplicationDbContext db,
            bool isClosed = false)
        {
            var fair = new JobFair
            {
                Title = "Existing Job Fair",
                Description = "Existing event description",
                Venue = "UAP Auditorium",
                StartsAtUtc = DateTime.UtcNow.AddDays(10),
                EndsAtUtc = DateTime.UtcNow.AddDays(11),
                CreatedByCoordinatorId = "coordinator-1",
                IsClosed = isClosed
            };

            db.JobFairs.Add(fair);
            await db.SaveChangesAsync();

            return fair;
        }

        [Test]
        public void OnlyCoordinatorRole_CanManageJobFairs()
        {
            var authorize = typeof(CoordinatorJobFairsController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.That(authorize, Is.Not.Null);

            Assert.That(
                authorize!.Roles,
                Is.EqualTo("UniversityCoordinator"));
        }

        [Test]
        public async Task CreateJobFair_SavesActiveEvent()
        {
            using var db = CreateContext();

            var controller = CreateController(db);

            var result = await controller.Create(ValidForm());

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var fair = await db.JobFairs.SingleAsync();

            Assert.That(
                fair.Title,
                Is.EqualTo("UAP Career Fair"));

            Assert.That(
                fair.IsClosed,
                Is.False);

            Assert.That(
                fair.CreatedByCoordinatorId,
                Is.EqualTo("coordinator-1"));
        }

        [Test]
        public async Task InvalidDates_CannotCreateJobFair()
        {
            using var db = CreateContext();

            var controller = CreateController(db);

            var form = ValidForm();

            // End date is earlier than start date.
            form.EndsAt = form.StartsAt.AddHours(-1);

            var result = await controller.Create(form);

            Assert.That(
                result,
                Is.TypeOf<ViewResult>());

            Assert.That(
                controller.ModelState.IsValid,
                Is.False);

            Assert.That(
                await db.JobFairs.CountAsync(),
                Is.Zero);
        }

        [Test]
        public async Task ActiveJobFair_CanBeEdited()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db);

            var controller = CreateController(db);

            var form = ValidForm();
            form.Title = "Updated Career Fair";
            form.Venue = "New Auditorium";

            var result = await controller.Edit(fair.Id, form);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var updated = await db.JobFairs.FindAsync(fair.Id);

            Assert.That(
                updated!.Title,
                Is.EqualTo("Updated Career Fair"));

            Assert.That(
                updated.Venue,
                Is.EqualTo("New Auditorium"));

            Assert.That(
                updated.IsClosed,
                Is.False);
        }

        [Test]
        public async Task ClosedJobFair_CannotOpenEditForm()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db, isClosed: true);

            var controller = CreateController(db);

            var result = await controller.Edit(fair.Id);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)result;

            Assert.That(
                redirect.ActionName,
                Is.EqualTo("Index"));
        }

        [Test]
        public async Task ClosedJobFair_CannotBeUpdated()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db, isClosed: true);

            var controller = CreateController(db);

            var form = ValidForm();
            form.Title = "Unauthorized Update";

            var result = await controller.Edit(fair.Id, form);

            Assert.That(
                result,
                Is.TypeOf<BadRequestObjectResult>());

            var saved = await db.JobFairs.FindAsync(fair.Id);

            Assert.That(
                saved!.Title,
                Is.EqualTo("Existing Job Fair"));

            Assert.That(
                saved.IsClosed,
                Is.True);
        }

        [Test]
        public async Task CloseJobFair_ChangesStatusToClosed()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db);

            var controller = CreateController(db);

            var result = await controller.Close(fair.Id);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var saved = await db.JobFairs.FindAsync(fair.Id);

            Assert.That(
                saved!.IsClosed,
                Is.True);
        }

        [Test]
        public async Task ClosingAlreadyClosedJobFair_DoesNotFail()
        {
            using var db = CreateContext();

            var fair = await AddJobFair(db, isClosed: true);

            var controller = CreateController(db);

            var result = await controller.Close(fair.Id);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var saved = await db.JobFairs.FindAsync(fair.Id);

            Assert.That(
                saved!.IsClosed,
                Is.True);

            Assert.That(
                await db.JobFairs.CountAsync(),
                Is.EqualTo(1));
        }
    }
}
