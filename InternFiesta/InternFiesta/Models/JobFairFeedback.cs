
using System;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class JobFairFeedback
    {
        public int Id { get; set; }

        public int JobFairRegistrationId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Comments { get; set; } = string.Empty;

        public DateTime SubmittedAtUtc { get; set; }
            = DateTime.UtcNow;
    }
}
