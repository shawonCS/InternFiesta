
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AcademicCompanyFeedback
    {
        public int Id { get; set; }

        public int AcademicInternshipId { get; set; }

        public AcademicInternship AcademicInternship { get; set; } = null!;

        [Required]
        public string CompanyUserId { get; set; } = string.Empty;

        public ApplicationUser CompanyUser { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(2000)]
        public string Comments { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
