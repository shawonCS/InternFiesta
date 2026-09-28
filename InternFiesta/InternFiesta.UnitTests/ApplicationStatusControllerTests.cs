using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace InternFiesta.UnitTests
{
    public class ApplicationStatusControllerTests
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

        [Test]
        public async Task MyApplications_ShouldReturnOnlyCurrentStudentsApplications()
        {
            using var context = CreateContext();

            var internship1 =
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

            var internship2 =
                new InternshipPosting
                {
                    Id = 2,
                    CompanyUserId = "company-1",
                    Title = "Web Intern",
                    Description = "Test internship",
                    RequiredSkills = "HTML",
                    Location = "Dhaka",
                    Deadline = DateTime.Now.AddDays(30),
                    Positions = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

            context.InternshipPostings.AddRange(
                internship1,
                internship2);

            context.InternshipApplications.AddRange(
                new InternshipApplication
                {
                    InternshipPostingId = 1,
                    StudentUserId = "student-1",
                    Status = "Applied",
                    AppliedAt = DateTime.Now
                },
                new InternshipApplication
                {
                    InternshipPostingId = 2,
                    StudentUserId = "student-1",
                    Status = "Shortlisted",
                    AppliedAt = DateTime.Now
                },
                new InternshipApplication
                {
                    InternshipPostingId = 1,
                    StudentUserId = "student-2",
                    Status = "Applied",
                    AppliedAt = DateTime.Now
                });

            await context.SaveChangesAsync();

            var userManager =
                CreateUserManager("student-1");

            var controller =
                new StudentInternshipsController(
                    context,
                    userManager);

            var result =
                await controller.MyApplications();

            var viewResult =
                result as ViewResult;

            Assert.That(
                viewResult,
                Is.Not.Null);

            var applications =
                viewResult!.Model
                as List<InternshipApplication>;

            Assert.That(
                applications,
                Is.Not.Null);

            Assert.That(
                applications!.Count,
                Is.EqualTo(2));

            Assert.That(
                applications.All(a =>
                    a.StudentUserId == "student-1"),
                Is.True);
        }

        [Test]
        public async Task MyApplications_ShouldReturnCorrectApplicationStatus()
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

            context.InternshipPostings.Add(
                internship);

            context.InternshipApplications.Add(
                new InternshipApplication
                {
                    InternshipPostingId = 1,
                    StudentUserId = "student-1",
                    Status = "Shortlisted",
                    AppliedAt = DateTime.Now
                });

            await context.SaveChangesAsync();

            var userManager =
                CreateUserManager("student-1");

            var controller =
                new StudentInternshipsController(
                    context,
                    userManager);

            var result =
                await controller.MyApplications();

            var viewResult =
                result as ViewResult;

            var applications =
                viewResult!.Model
                as List<InternshipApplication>;

            Assert.That(
                applications,
                Is.Not.Null);

            Assert.That(
                applications!.Count,
                Is.EqualTo(1));

            Assert.That(
                applications[0].Status,
                Is.EqualTo("Shortlisted"));
        }
    }
}