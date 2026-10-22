
namespace InternFiesta.ViewModels.Alumni
{
    public class AlumniPublicProfile
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";

        public string Industry { get; set; } = "";

        public string? JobTitle { get; set; }

        public string? CompanyName { get; set; }

        public string? About { get; set; }

        public string? Skills { get; set; }

        public string? LinkedInUrl { get; set; }

        public bool IsAvailableForMentoring { get; set; }
    }
}
