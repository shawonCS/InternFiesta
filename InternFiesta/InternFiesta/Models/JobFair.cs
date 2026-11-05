using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class JobFair
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Venue { get; set; } = string.Empty;

        public DateTime StartsAtUtc { get; set; }

        public DateTime EndsAtUtc { get; set; }

        public bool IsClosed { get; set; } = false;

        [Required]
        public string CreatedByCoordinatorId { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<JobFairRegistration> Registrations { get; set; }
            = new List<JobFairRegistration>();
    }
}