namespace InternFiesta.Services
{
    public class CandidateMatchResult
    {
        public int Score { get; set; }

        public string Label { get; set; } = string.Empty;

        public int MatchedSkills { get; set; }

        public int RequiredSkills { get; set; }
    }

    public static class CandidateMatchService
    {
        public static CandidateMatchResult Calculate(
            string? requiredSkills,
            string? studentSkills)
        {
            var required =
                ParseSkills(requiredSkills);

            var student =
                ParseSkills(studentSkills);

            if (required.Count == 0)
            {
                return new CandidateMatchResult
                {
                    Score = 0,
                    Label = "Not Rated",
                    MatchedSkills = 0,
                    RequiredSkills = 0
                };
            }

            var matched =
                required.Count(skill =>
                    student.Contains(skill));

            var score =
                (int)Math.Round(
                    matched * 100.0 /
                    required.Count);

            var label =
                score >= 70 ? "High Match" :
                score >= 40 ? "Medium Match" :
                score > 0 ? "Low Match" :
                "No Match";

            return new CandidateMatchResult
            {
                Score = score,
                Label = label,
                MatchedSkills = matched,
                RequiredSkills = required.Count
            };
        }

        private static HashSet<string> ParseSkills(
            string? skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
            {
                return new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
            }

            var separators =
                new[]
                {
                    ',',
                    ';',
                    '|',
                    '\n',
                    '\r'
                };

            return skills
                .Split(
                    separators,
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s =>
                    !string.IsNullOrWhiteSpace(s))
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);
        }
    }
}