using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class InternshipPosting
    {
        public int Id { get; set; }

        [Required]
        public string CompanyUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; }

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; }

        [Range(1, 100)]
        public int Positions { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? CompanyUser { get; set; }
    }
}