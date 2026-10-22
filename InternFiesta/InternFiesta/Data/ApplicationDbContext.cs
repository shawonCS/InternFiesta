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
            builder.Entity<FacultyEvaluation>()
                .HasOne(e => e.AcademicInternship)
                .WithMany()
                .HasForeignKey(e => e.AcademicInternshipId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FacultyEvaluation>()
                .HasIndex(e => e.AcademicInternshipId)
                .IsUnique();

            builder.Entity<JobFairRegistration>()
                .HasOne(r => r.JobFair)
                .WithMany(f => f.Registrations)
                .HasForeignKey(r => r.JobFairId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobFairRegistration>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobFairRegistration>()
                .HasIndex(r => new { r.JobFairId, r.UserId })
                .IsUnique();
            builder.Entity<JobFairBooth>()
                .HasOne<JobFair>()
                .WithMany()
                .HasForeignKey(b => b.JobFairId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobFairBooth>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(b => b.CompanyUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobFairBooth>()
                .HasIndex(b => new { b.JobFairId, b.CompanyUserId })
                .IsUnique();

            builder.Entity<JobFairFeedback>()
                .HasOne<JobFairRegistration>()
                .WithMany()
                .HasForeignKey(f => f.JobFairRegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobFairFeedback>()
                .HasIndex(f => f.JobFairRegistrationId)
                .IsUnique();


            builder.Entity<AlumniProfile>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<AlumniProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AlumniProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<AlumniMentoringRequest>()
                .HasOne(r => r.AlumniProfile)
                .WithMany(p => p.MentoringRequests)
                .HasForeignKey(r => r.AlumniProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AlumniMentoringRequest>()
                .HasOne(r => r.StudentUser)
                .WithMany()
                .HasForeignKey(r => r.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Only one pending request per student-mentor pair.
            // SQL Server filtered unique index.
            builder.Entity<AlumniMentoringRequest>()
                .HasIndex(r => new
                {
                    r.AlumniProfileId,
                    r.StudentUserId
                })
                .IsUnique()
                .HasFilter("[Status] = 'Pending'");

        }
        public DbSet<InterviewSchedule> InterviewSchedules { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AcademicInternship> AcademicInternships { get; set; }
        public DbSet<AcademicMilestone> AcademicMilestones { get; set; }
        public DbSet<AcademicProgressUpdate> AcademicProgressUpdates { get; set; }
        public DbSet<AcademicCompanyFeedback> AcademicCompanyFeedbacks { get; set; }
        public DbSet<FacultyEvaluation> FacultyEvaluations { get; set; } = null!;

        public DbSet<JobFair> JobFairs { get; set; } = null!;

        public DbSet<JobFairRegistration> JobFairRegistrations { get; set; } = null!;
        public DbSet<JobFairBooth> JobFairBooths { get; set; } = null!;

        public DbSet<JobFairFeedback> JobFairFeedbacks { get; set; } = null!;
        public DbSet<AlumniProfile> AlumniProfiles { get; set; }
    = null!;

        public DbSet<AlumniMentoringRequest> AlumniMentoringRequests
        { get; set; } = null!;
    }
}