
using System;
using System.Threading.Tasks;
using InternFiesta.Data;
using InternFiesta.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    [TestFixture]
    public class CoordinatorOverviewServiceTests
    {
        [Test]
        public async Task GetCountsAsync_EmptyDatabase_ReturnsAllZeros()
        {
            // STEP 1: Create a unique temporary database.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    "CoordinatorEmptyTest_" + Guid.NewGuid())
                .Options;

            await using var context =
                new ApplicationDbContext(options);

            // STEP 2: Do not insert any records.
            // Every table starts empty.

            // STEP 3: Execute the real aggregate service.
            var service = new CoordinatorOverviewService(context);

            var result = await service.GetCountsAsync();

            // STEP 4: Verify all six counts are zero.
            Assert.Multiple(() =>
            {
                Assert.That(
                    result.StudentCount,
                    Is.EqualTo(0),
                    "Student count should be zero.");

                Assert.That(
                    result.CompanyCount,
                    Is.EqualTo(0),
                    "Company count should be zero.");

                Assert.That(
                    result.InternshipCount,
                    Is.EqualTo(0),
                    "Internship count should be zero.");

                Assert.That(
                    result.ApplicationCount,
                    Is.EqualTo(0),
                    "Application count should be zero.");

                Assert.That(
                    result.JobFairCount,
                    Is.EqualTo(0),
                    "Job Fair count should be zero.");

                Assert.That(
                    result.AcademicParticipationCount,
                    Is.EqualTo(0),
                    "Academic participation count should be zero.");
            });
        }
    }
}
