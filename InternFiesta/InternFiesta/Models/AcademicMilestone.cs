
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AcademicMilestone
    {
        public int Id { get; set; }

        public int AcademicInternshipId { get; set; }

        public AcademicInternship AcademicInternship { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime? CompletedAt { get; set; }
    }
}
