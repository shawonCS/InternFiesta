
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AcademicInternship
    {
        public int Id { get; set; }

        [Required]
        public int InternshipApplicationId { get; set; }

        public InternshipApplication InternshipApplication { get; set; } = null!;

        // Faculty user assigned to supervise this student
        [Required]
        public string FacultyUserId { get; set; } = string.Empty;

        public ApplicationUser FacultyUser { get; set; } = null!;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AcademicMilestone> Milestones { get; set; }
            = new List<AcademicMilestone>();

        public ICollection<AcademicProgressUpdate> ProgressUpdates { get; set; }
            = new List<AcademicProgressUpdate>();

        public ICollection<AcademicCompanyFeedback> CompanyFeedback { get; set; }
            = new List<AcademicCompanyFeedback>();
    }
}
