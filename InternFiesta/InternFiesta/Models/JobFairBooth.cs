
using System;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class JobFairBooth
    {
        public int Id { get; set; }

        public int JobFairId { get; set; }

        [Required]
        public string CompanyUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string BoothName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(100)]
        public string? BoothLocation { get; set; }
    }
}
