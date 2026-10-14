using System;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class JobFairRegistration
    {
        public int Id { get; set; }

        public int JobFairId { get; set; }

        public JobFair JobFair { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string ParticipantRole { get; set; } = string.Empty;

        public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    }
}