using InternFiesta.Models;
using InternFiesta.Services;
using NUnit.Framework;

namespace InternFiesta.UnitTests
{
    public class ProfileCompletenessTests
    {
        [Test]
        public void EmptyProfile_ShouldNotAllowApplication()
        {
            var profile = new StudentProfile();

            var result = ProfileCompletenessService.Calculate(profile);

            Assert.That(result.Percentage, Is.EqualTo(0));
            Assert.That(result.CanApply, Is.False);
        }

        [Test]
        public void RequiredFieldsComplete_ShouldAllowApplication()
        {
            var profile = new StudentProfile
            {
                StudentId = "23201021",
                Department = "CSE",
                CGPA = 3,
                Skills = "C#, SQL"
            };

            var result = ProfileCompletenessService.Calculate(profile);

            Assert.That(result.Percentage, Is.EqualTo(67));
            Assert.That(result.CanApply, Is.True);
        }
    }
}