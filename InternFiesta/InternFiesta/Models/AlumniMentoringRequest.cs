
using System;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AlumniMentoringRequest
    {
        public int Id { get; set; }

        public int AlumniProfileId { get; set; }

        public AlumniProfile AlumniProfile { get; set; } = null!;

        [Required]
        public string StudentUserId { get; set; } = string.Empty;

        public ApplicationUser StudentUser { get; set; } = null!;

        [Required]
        [StringLength(800)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;

        public DateTime? RespondedAtUtc { get; set; }
    }
}
