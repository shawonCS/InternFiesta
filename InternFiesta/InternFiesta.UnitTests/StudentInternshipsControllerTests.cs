using InternFiesta.Controllers;
using InternFiesta.Data;
using InternFiesta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    public class StudentInternshipsControllerTests
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

        [Test]
        public async Task Index_ShouldReturnOnlyActiveInternships()
        {
            // Arrange
            using var context = CreateContext();

            context.InternshipPostings.AddRange(
                new InternshipPosting
                {
                    CompanyUserId = "company-1",
                    Title = "Active Internship",
                    Description = "Active test internship",
                    RequiredSkills = "C#",
                    Location = "Dhaka",
                    Deadline = DateTime.Now.AddDays(10),
                    Positions = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new InternshipPosting
                {
                    CompanyUserId = "company-1",
                    Title = "Closed Internship",
                    Description = "Closed test internship",
                    RequiredSkills = "Java",
                    Location = "Dhaka",
                    Deadline = DateTime.Now.AddDays(10),
                    Positions = 1,
                    IsActive = false,
                    CreatedAt = DateTime.Now
                }
            );

            await context.SaveChangesAsync();

            var controller =
                new StudentInternshipsController(context,null!);

            // Act
            var result =
                await controller.Index(null, null, null);

            // Assert
            var viewResult =
                result as ViewResult;

            Assert.That(viewResult, Is.Not.Null);

            var internships =
                viewResult!.Model
                as IEnumerable<InternshipPosting>;

            Assert.That(internships, Is.Not.Null);

            var list = internships!.ToList();

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(
                list[0].Title,
                Is.EqualTo("Active Internship"));
        }

        [Test]
        public async Task Index_ShouldFilterInternships()
        {
            // Arrange
            using var context = CreateContext();

            context.InternshipPostings.AddRange(
                new InternshipPosting
                {
                    CompanyUserId = "company-1",
                    Title = "Software Developer Intern",
                    Description = "Development internship",
                    RequiredSkills = "C#, SQL",
                    Location = "Dhaka",
                    Deadline = DateTime.Now.AddDays(15),
                    Positions = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new InternshipPosting
                {
                    CompanyUserId = "company-2",
                    Title = "Network Intern",
                    Description = "Networking internship",
                    RequiredSkills = "Cisco",
                    Location = "Chattogram",
                    Deadline = DateTime.Now.AddDays(20),
                    Positions = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            );

            await context.SaveChangesAsync();

            var controller =
                new StudentInternshipsController(context, null!);

            // Act
            var result =
                await controller.Index(
                    "Software",
                    "Dhaka",
                    "SQL");

            // Assert
            var viewResult =
                result as ViewResult;

            Assert.That(viewResult, Is.Not.Null);

            var internships =
                viewResult!.Model
                as IEnumerable<InternshipPosting>;

            Assert.That(internships, Is.Not.Null);

            var list = internships!.ToList();

            Assert.That(list.Count, Is.EqualTo(1));

            Assert.That(
                list[0].Title,
                Is.EqualTo("Software Developer Intern"));
        }
    }
}