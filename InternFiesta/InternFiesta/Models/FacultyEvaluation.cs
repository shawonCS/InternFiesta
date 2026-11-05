
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class FacultyEvaluation
    {
        public int Id { get; set; }

        [Required]
        public int AcademicInternshipId { get; set; }

        public AcademicInternship AcademicInternship { get; set; } = null!;

        [Required]
        public string FacultyUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string Grade { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Comments { get; set; } = string.Empty;

        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
