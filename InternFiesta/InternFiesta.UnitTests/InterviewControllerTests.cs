using System.Security.Claims;
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

namespace InternFiesta.UnitTests
{
    public class InterviewControllerTests
    {
        [Test]
        public async Task Schedule_ShouldCreateInterviewForShortlistedStudent()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var company = new ApplicationUser
            {
                Id = "company-1",
                UserName = "company@test.com",
                Email = "company@test.com"
            };

            var student = new ApplicationUser
            {
                Id = "student-1",
                UserName = "student@test.com",
                Email = "student@test.com"
            };

            context.Users.AddRange(company, student);

            var internship = new InternshipPosting
            {
                Id = 1,
                CompanyUserId = company.Id,
                Title = "Test Internship",
                Description = "Test",
                RequiredSkills = "C#",
                Location = "Dhaka",
                Deadline = DateTime.Now.AddDays(30),
                Positions = 1,
                IsActive = true
            };

            context.InternshipPostings.Add(internship);

            var application = new InternshipApplication
            {
                Id = 1,
                InternshipPostingId = internship.Id,
                InternshipPosting = internship,
                StudentUserId = student.Id,
                Student = student,
                Status = "Shortlisted",
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
                .Returns(company.Id);

            var controller = new InterviewController(
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

            var model = new InterviewSchedule
            {
                InternshipApplicationId = application.Id,
                InterviewDateTime = DateTime.Now.AddDays(2),
                LocationOrLink = "https://meet.google.com/test",
                Notes = "Test interview"
            };

            var result = await controller.Schedule(model);

            var savedInterview = await context.InterviewSchedules
                .FirstOrDefaultAsync();

            Assert.That(savedInterview, Is.Not.Null);
            Assert.That(
                savedInterview!.InternshipApplicationId,
                Is.EqualTo(application.Id));

            Assert.That(
                result,
                Is.TypeOf<RedirectToActionResult>());
        }

        [Test]
        public async Task Schedule_NonShortlistedStudent_ShouldReturnBadRequest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var company = new ApplicationUser
            {
                Id = "company-1",
                UserName = "company@test.com",
                Email = "company@test.com"
            };

            var student = new ApplicationUser
            {
                Id = "student-1",
                UserName = "student@test.com",
                Email = "student@test.com"
            };

            context.Users.AddRange(company, student);

            var internship = new InternshipPosting
            {
                Id = 1,
                CompanyUserId = company.Id,
                Title = "Test Internship",
                Description = "Test",
                RequiredSkills = "C#",
                Location = "Dhaka",
                Deadline = DateTime.Now.AddDays(30),
                Positions = 1,
                IsActive = true
            };

            context.InternshipPostings.Add(internship);

            var application = new InternshipApplication
            {
                Id = 1,
                InternshipPostingId = internship.Id,
                InternshipPosting = internship,
                StudentUserId = student.Id,
                Student = student,
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
                .Returns(company.Id);

            var controller = new InterviewController(
                context,
                userManager.Object);

            var model = new InterviewSchedule
            {
                InternshipApplicationId = application.Id,
                InterviewDateTime = DateTime.Now.AddDays(2),
                LocationOrLink = "Dhaka"
            };

            var result = await controller.Schedule(model);

            Assert.That(
                result,
                Is.TypeOf<BadRequestResult>());
        }
    }
}