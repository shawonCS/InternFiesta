
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password),
            ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student";

        // Optional Alumni fields
        [StringLength(100)]
        public string? AlumniIndustry { get; set; }

        [StringLength(120)]
        [Display(Name = "Job Title")]
        public string? AlumniJobTitle { get; set; }

        [StringLength(150)]
        [Display(Name = "Company / Organization")]
        public string? AlumniCompanyName { get; set; }

        [StringLength(1000)]
        [Display(Name = "About")]
        public string? AlumniAbout { get; set; }

        [StringLength(300)]
        [Display(Name = "Skills")]
        public string? AlumniSkills { get; set; }

        [StringLength(250)]
        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "LinkedIn URL")]
        public string? AlumniLinkedInUrl { get; set; }
    }
}
