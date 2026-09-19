using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace InternFiesta.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
    } //Identity handles the password hash.
}
