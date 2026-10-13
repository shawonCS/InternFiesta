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
        public DbSet<SavedInternship> SavedInternships { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<InternshipPosting>()
            .HasQueryFilter(i => !i.IsDeleted);

            builder.Entity<InternshipApplication>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentUserId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.Entity<InterviewSchedule>()
                .HasOne(i =>i.InternshipApplication)
                .WithOne()
                .HasForeignKey<InterviewSchedule>(i => i.InternshipApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.Entity<AcademicInternship>()
                .HasOne(a => a.InternshipApplication)
                .WithMany()
                .HasForeignKey(a => a.InternshipApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AcademicInternship>()
                .HasIndex(a => a.InternshipApplicationId)
                .IsUnique();

            builder.Entity<AcademicInternship>()
                .HasOne(a => a.FacultyUser)
                .WithMany()
                .HasForeignKey(a => a.FacultyUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AcademicCompanyFeedback>()
                .HasOne(f => f.CompanyUser)
                .WithMany()
                .HasForeignKey(f => f.CompanyUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<InterviewSchedule> InterviewSchedules { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AcademicInternship> AcademicInternships { get; set; }
        public DbSet<AcademicMilestone> AcademicMilestones { get; set; }
        public DbSet<AcademicProgressUpdate> AcademicProgressUpdates { get; set; }
        public DbSet<AcademicCompanyFeedback> AcademicCompanyFeedbacks { get; set; }
    }
}