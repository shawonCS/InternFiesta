
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.ViewModels.Alumni
{
    public class AlumniProfileForm
    {
        [StringLength(100)]
        public string? Industry { get; set; }

        [StringLength(120)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [StringLength(150)]
        [Display(Name = "Company / Organization")]
        public string? CompanyName { get; set; }

        [StringLength(1000)]
        [Display(Name = "About")]
        public string? About { get; set; }

        [StringLength(300)]
        public string? Skills { get; set; }

        [StringLength(250)]
        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "LinkedIn URL")]
        public string? LinkedInUrl { get; set; }

        [Display(Name = "Show my profile to students")]
        public bool IsVisibleToStudents { get; set; }

        [Display(Name = "Available for mentoring")]
        public bool IsAvailableForMentoring { get; set; }
    }
}
