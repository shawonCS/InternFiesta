
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AcademicProgressUpdate
    {
        public int Id { get; set; }

        public int AcademicInternshipId { get; set; }

        public AcademicInternship AcademicInternship { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
