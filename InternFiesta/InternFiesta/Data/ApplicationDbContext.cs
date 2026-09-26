using InternFiesta.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InternFiesta.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<StudentProfile> StudentProfiles { get; set; }

        public DbSet<InternshipPosting> InternshipPostings { get; set; }

        public DbSet<InternshipApplication> InternshipApplications { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<InternshipApplication>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}