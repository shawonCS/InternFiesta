
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
    public class InternshipApplicationControllerTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            return new ApplicationDbContext(options);
        }

        private UserManager<ApplicationUser> CreateUserManager(
            string userId)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

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
                .Setup(u => u.GetUserId(
                    It.IsAny<ClaimsPrincipal>()))
                .Returns(userId);

            return userManager.Object;
        }

        private StudentInternshipsController CreateController(
            ApplicationDbContext context,
            string userId)
        {
            var controller = new StudentInternshipsController(
                context,
                CreateUserManager(userId));

            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Test]
        public async Task Apply_ShouldCreateApplication()
        {
            using var context = CreateContext();

            context.InternshipPostings.Add(
                new InternshipPosting
                {
                    Id = 1,
                    CompanyUserId = "company-1",
                    Title = "Software Intern",
                    Description = "Test internship",
                    RequiredSkills = "C#",
                    Location = "Dhaka",
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Positions = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            // Student must have a complete profile
            // before applying.
            context.StudentProfiles.Add(
                new StudentProfile
                {
                    UserId = "student-1",
                    StudentId = "STU-001",
                    Department = "Computer Science",
                    Skills = "C#, ASP.NET Core",
                    CGPA = 3.50m,
                    Projects = "InternFiesta"
                });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                "student-1");

            var result = await controller.Apply(1);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var applications =
                await context.InternshipApplications.ToListAsync();

            Assert.That(applications.Count, Is.EqualTo(1));

            Assert.That(
                applications[0].InternshipPostingId,
                Is.EqualTo(1));

            Assert.That(
                applications[0].StudentUserId,
                Is.EqualTo("student-1"));

            Assert.That(
                controller.TempData["SuccessMessage"],
                Is.EqualTo(
                    "Your application was submitted successfully."));
        }

        [Test]
        public async Task Apply_ShouldPreventDuplicateApplication()
        {
            using var context = CreateContext();

            context.InternshipPostings.Add(
                new InternshipPosting
                {
                    Id = 1,
                    CompanyUserId = "company-1",
                    Title = "Software Intern",
                    Description = "Test internship",
                    RequiredSkills = "C#",
                    Location = "Dhaka",
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Positions = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            context.InternshipApplications.Add(
                new InternshipApplication
                {
                    InternshipPostingId = 1,
                    StudentUserId = "student-1",
                    Status = "Applied",
                    AppliedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                "student-1");

            var result = await controller.Apply(1);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var applications =
                await context.InternshipApplications.ToListAsync();

            Assert.That(applications.Count, Is.EqualTo(1));

            Assert.That(
                controller.TempData["WarningMessage"],
                Is.EqualTo(
                    "You have already applied for this internship."));
        }
    }
}
