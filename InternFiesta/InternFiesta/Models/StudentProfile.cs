using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace InternFiesta.Models
{
    public class StudentProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Range(0, 4)]
        [Precision(3, 2)]
        public decimal? CGPA { get; set; }

        public string? Skills { get; set; }

        public string? Projects { get; set; }

        public string? Experience { get; set; }

        public ApplicationUser? User { get; set; }
    }
}