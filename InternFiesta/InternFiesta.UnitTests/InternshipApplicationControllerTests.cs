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
    public class InternshipApplicationControllerTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(
                        Guid.NewGuid().ToString())
                    .Options;

            return new ApplicationDbContext(options);
        }

        private UserManager<ApplicationUser>
            CreateUserManager(string userId)
        {
            var store =
                new Mock<IUserStore<ApplicationUser>>();

            var userManager =
                new Mock<UserManager<ApplicationUser>>(
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
                .Setup(u =>
                    u.GetUserId(
                        It.IsAny<ClaimsPrincipal>()))
                .Returns(userId);

            return userManager.Object;
        }

        private StudentInternshipsController CreateController(
            ApplicationDbContext context,
            string userId)
        {
            var userManager =
                CreateUserManager(userId);

            var controller =
                new StudentInternshipsController(
                    context,
                    userManager);

            controller.TempData =
                new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Test]
        public async Task Apply_ShouldCreateApplication()
        {
            using var context = CreateContext();

            var internship =
                new InternshipPosting
                {
                    Id = 1,
                    CompanyUserId = "company-1",
                    Title = "Software Intern",
                    Description = "Test internship",
                    RequiredSkills = "C#",
                    Location = "Dhaka",
                    Deadline = DateTime.Now.AddDays(30),
                    Positions = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

            context.InternshipPostings.Add(internship);

            await context.SaveChangesAsync();

            var controller =
                CreateController(
                    context,
                    "student-1");

            var result =
                await controller.Apply(1);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var applications =
                await context.InternshipApplications
                    .ToListAsync();

            Assert.That(
                applications.Count,
                Is.EqualTo(1));

            Assert.That(
                applications[0].InternshipPostingId,
                Is.EqualTo(1));

            Assert.That(
                applications[0].StudentUserId,
                Is.EqualTo("student-1"));
        }

        [Test]
        public async Task Apply_ShouldPreventDuplicateApplication()
        {
            using var context = CreateContext();

            var internship =
                new InternshipPosting
                {
                    Id = 1,
                    CompanyUserId = "company-1",
                    Title = "Software Intern",
                    Description = "Test internship",
                    RequiredSkills = "C#",
                    Location = "Dhaka",
                    Deadline = DateTime.Now.AddDays(30),
                    Positions = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

            context.InternshipPostings.Add(internship);

            context.InternshipApplications.Add(
                new InternshipApplication
                {
                    InternshipPostingId = 1,
                    StudentUserId = "student-1",
                    Status = "Applied",
                    AppliedAt = DateTime.Now
                });

            await context.SaveChangesAsync();

            var controller =
                CreateController(
                    context,
                    "student-1");

            var result =
                await controller.Apply(1);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var applications =
                await context.InternshipApplications
                    .ToListAsync();

            Assert.That(
                applications.Count,
                Is.EqualTo(1));

            Assert.That(
                controller.TempData["ApplicationError"],
                Is.EqualTo(
                    "You have already applied for this internship."));
        }
    }
}