using InternFiesta.Models;

namespace InternFiesta.Services
{
    public class ProfileCompletenessResult
    {
        public int Percentage { get; set; }

        public List<string> MissingFields { get; set; }
            = new();

        public List<string> MissingRequiredFields { get; set; }
            = new();

        public bool CanApply =>
            MissingRequiredFields.Count == 0;
    }

    public static class ProfileCompletenessService
    {
        public static ProfileCompletenessResult Calculate(
            StudentProfile? profile)
        {
            if (profile == null)
            {
                return new ProfileCompletenessResult
                {
                    Percentage = 0,

                    MissingFields = new List<string>
                    {
                        "Student ID",
                        "Department",
                        "CGPA",
                        "Skills",
                        "Projects",
                        "Experience"
                    },

                    MissingRequiredFields = new List<string>
                    {
                        "Student ID",
                        "Department",
                        "CGPA",
                        "Skills"
                    }
                };
            }

            var fields =
                new[]
                {
                    new ProfileField(
                        "Student ID",
                        profile.StudentId,
                        true),

                    new ProfileField(
                        "Department",
                        profile.Department,
                        true),

                    new ProfileField(
                        "CGPA",
                        profile.CGPA,
                        true),

                    new ProfileField(
                        "Skills",
                        profile.Skills,
                        true),

                    new ProfileField(
                        "Projects",
                        profile.Projects,
                        false),

                    new ProfileField(
                        "Experience",
                        profile.Experience,
                        false)
                };

            var missingFields =
                fields
                    .Where(f =>
                        !HasValue(f.Value))
                    .Select(f => f.Name)
                    .ToList();

            var missingRequiredFields =
                fields
                    .Where(f =>
                        f.RequiredForApply &&
                        !HasValue(f.Value))
                    .Select(f => f.Name)
                    .ToList();

            var completed =
                fields.Length -
                missingFields.Count;

            var percentage =
                (int)Math.Round(
                    completed * 100.0 /
                    fields.Length);

            return new ProfileCompletenessResult
            {
                Percentage = percentage,
                MissingFields = missingFields,
                MissingRequiredFields =
                    missingRequiredFields
            };
        }

        private static bool HasValue(
            object? value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is string text)
            {
                return !string.IsNullOrWhiteSpace(
                    text);
            }

            if (value is decimal decimalValue)
            {
                return decimalValue > 0;
            }

            if (value is double doubleValue)
            {
                return doubleValue > 0;
            }

            if (value is float floatValue)
            {
                return floatValue > 0;
            }

            return true;
        }

        private class ProfileField
        {
            public string Name { get; }

            public object? Value { get; }

            public bool RequiredForApply { get; }

            public ProfileField(
                string name,
                object? value,
                bool requiredForApply)
            {
                Name = name;
                Value = value;
                RequiredForApply =
                    requiredForApply;
            }
        }
    }
}