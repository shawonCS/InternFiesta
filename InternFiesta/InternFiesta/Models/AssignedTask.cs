
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AssignedTask
    {
        public int Id { get; set; }

        public int AcademicInternshipId { get; set; }

        public AcademicInternship AcademicInternship { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Assigned";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public ICollection<AcademicProgressUpdate> ProgressUpdates
        { get; set; } = new List<AcademicProgressUpdate>();
    }
}
