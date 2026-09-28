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
    public class SavedInternshipControllerTests
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
        public async Task Save_ShouldCreateSavedInternship()
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

            var userManager =
                CreateUserManager("student-1");

            var controller =
                new StudentInternshipsController(
                    context,
                    userManager);

            var result =
                await controller.Save(1);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var savedInternships =
                await context.SavedInternships
                    .ToListAsync();

            Assert.That(
                savedInternships.Count,
                Is.EqualTo(1));

            Assert.That(
                savedInternships[0].InternshipPostingId,
                Is.EqualTo(1));

            Assert.That(
                savedInternships[0].StudentUserId,
                Is.EqualTo("student-1"));
        }

        [Test]
        public async Task RemoveSaved_ShouldDeleteSavedInternship()
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

            var savedInternship =
                new SavedInternship
                {
                    Id = 1,
                    InternshipPostingId = 1,
                    StudentUserId = "student-1",
                    SavedAt = DateTime.Now
                };

            context.SavedInternships.Add(
                savedInternship);

            await context.SaveChangesAsync();

            var userManager =
                CreateUserManager("student-1");

            var controller =
                new StudentInternshipsController(
                    context,
                    userManager);

            var result =
                await controller.RemoveSaved(1);

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());

            var savedInternships =
                await context.SavedInternships
                    .ToListAsync();

            Assert.That(
                savedInternships.Count,
                Is.EqualTo(0));
        }
    }
}