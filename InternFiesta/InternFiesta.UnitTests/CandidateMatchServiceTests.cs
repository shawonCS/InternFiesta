using InternFiesta.Services;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    public class CandidateMatchServiceTests
    {
        [Test]
        public void MatchingSkills_ShouldGiveHighMatch()
        {
            var result = CandidateMatchService.Calculate(
                "C#, SQL",
                "C#, SQL");

            Assert.That(result.Score, Is.EqualTo(100));
            Assert.That(result.Label, Is.EqualTo("High Match"));
        }

        [Test]
        public void NoMatchingSkills_ShouldGiveNoMatch()
        {
            var result = CandidateMatchService.Calculate(
                "C#, SQL",
                "Java, Python");

            Assert.That(result.Score, Is.EqualTo(0));
            Assert.That(result.Label, Is.EqualTo("No Match"));
        }
    }
}