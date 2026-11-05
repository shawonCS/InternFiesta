
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Administrative suspension.
        // Independent from temporary password lockout.
        public bool IsDisabled { get; set; } = false;

        [StringLength(80)]
        public string? ProfilePhotoFileName { get; set; }
    }
}
