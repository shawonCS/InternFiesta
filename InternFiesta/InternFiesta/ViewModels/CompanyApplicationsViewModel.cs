using InternFiesta.Models;

namespace InternFiesta.ViewModels
{
    public class CompanyApplicationsViewModel
    {
        public InternshipPosting Internship { get; set; } = null!;

        public List<CompanyApplicantViewModel> Applicants { get; set; }
            = new();
    }

    public class CompanyApplicantViewModel
    {
        public InternshipApplication Application { get; set; } = null!;

        public ApplicationUser? Student { get; set; }

        public StudentProfile? Profile { get; set; }
        public int MatchScore { get; set; }

        public string MatchLabel { get; set; }
            = "Not Rated";
    }
}