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
using System.Security.Claims;

namespace InternFiesta.UnitTests
{
    public class CompanyApplicationStatusTests
    {
        [Test]
        public async Task UpdateStatus_ShouldShortlistApplication()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var internship = new InternshipPosting
            {
                CompanyUserId = "company-1",
                Title = "Test Internship",
                Description = "Test",
                RequiredSkills = "C#",
                Location = "Dhaka",
                Deadline = DateTime.Now.AddDays(30),
                Positions = 1,
                IsActive = true
            };

            context.InternshipPostings.Add(internship);
            await context.SaveChangesAsync();

            var application = new InternshipApplication
            {
                InternshipPostingId = internship.Id,
                StudentUserId = "student-1",
                Status = "Applied",
                AppliedAt = DateTime.Now
            };

            context.InternshipApplications.Add(application);
            await context.SaveChangesAsync();

            var store = new Mock<IUserStore<ApplicationUser>>();

            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("company-1");

            var controller = new CompanyApplicationsController(
                context,
                userManager.Object);

            var httpContext = new DefaultHttpContext();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());

            var result = await controller.UpdateStatus(
                application.Id,
                "Shortlisted");

            Assert.That(application.Status, Is.EqualTo("Shortlisted"));
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }

        [Test]
        public async Task InvalidStatus_ShouldReturnBadRequest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();

            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("company-1");

            var controller = new CompanyApplicationsController(
                context,
                userManager.Object);

            var result = await controller.UpdateStatus(
                1,
                "WrongStatus");

            Assert.That(result, Is.TypeOf<BadRequestResult>());
        }
    }
}