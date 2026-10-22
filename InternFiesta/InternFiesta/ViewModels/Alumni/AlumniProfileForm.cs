
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.ViewModels.Alumni
{
    public class AlumniProfileForm
    {
        [Required(ErrorMessage = "Professional headline is required.")]
        [StringLength(180)]
        [Display(Name = "Professional Headline")]
        public string Headline { get; set; } = string.Empty;

        [Required(ErrorMessage = "Industry is required.")]
        [StringLength(100)]
        public string Industry { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Current Job Title")]
        public string? JobTitle { get; set; }

        [StringLength(150)]
        [Display(Name = "Company / Organization")]
        public string? CompanyName { get; set; }

        [StringLength(1000)]
        [Display(Name = "About Me")]
        public string? About { get; set; }

        [StringLength(300)]
        public string? Skills { get; set; }

        [Url(ErrorMessage = "Enter a valid URL.")]
        [StringLength(250)]
        [Display(Name = "LinkedIn Profile URL")]
        public string? LinkedInUrl { get; set; }

        [Display(Name = "Allow Students to Discover My Profile")]
        public bool IsVisibleToStudents { get; set; }

        [Display(Name = "Available for Mentoring")]
        public bool IsAvailableForMentoring { get; set; }
    }
}
