
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AlumniProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(180)]
        public string Headline { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Industry { get; set; } = string.Empty;

        [StringLength(120)]
        public string? JobTitle { get; set; }

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [StringLength(1000)]
        public string? About { get; set; }

        [StringLength(300)]
        public string? Skills { get; set; }

        [StringLength(250)]
        public string? LinkedInUrl { get; set; }

        // Private by default.
        public bool IsVisibleToStudents { get; set; } = false;

        public bool IsAvailableForMentoring { get; set; } = false;

        public DateTime UpdatedAtUtc { get; set; }
            = DateTime.UtcNow;

        public ICollection<AlumniMentoringRequest> MentoringRequests
        { get; set; } = new List<AlumniMentoringRequest>();
    }
}
