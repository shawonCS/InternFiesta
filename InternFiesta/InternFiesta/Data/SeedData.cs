using InternFiesta.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace InternFiesta.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();
            var context =
                services.GetRequiredService<ApplicationDbContext>();
            // -------------------------
            // Company test account
            // -------------------------
            const string companyEmail = "company@test.com";
            const string companyPassword = "Company@123";

            var company =
                await userManager.FindByEmailAsync(companyEmail);

            if (company == null)
            {
                company = new ApplicationUser
                {
                    UserName = companyEmail,
                    Email = companyEmail,
                    FullName = "Test Company",
                    EmailConfirmed = true
                };

                var companyResult =
                    await userManager.CreateAsync(
                        company,
                        companyPassword);

                if (companyResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        company,
                        "Company");
                }
            }

            // -------------------------
            // Student test account
            // -------------------------
            const string studentEmail = "student@test.com";
            const string studentPassword = "Student@123";

            var student =
                await userManager.FindByEmailAsync(studentEmail);

            if (student == null)
            {
                student = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FullName = "Test Student",
                    EmailConfirmed = true
                };

                var studentResult =
                    await userManager.CreateAsync(
                        student,
                        studentPassword);

                if (studentResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        student,
                        "Student");
                }
            }
            // -------------------------
            // Application status test data
            // -------------------------

            if (company != null && student != null)
            {
                var statusInternship =
                    await context.InternshipPostings
                        .FirstOrDefaultAsync(i =>
                            i.Title == "Application Status Test Internship");

                if (statusInternship == null)
                {
                    statusInternship = new InternshipPosting
                    {
                        CompanyUserId = company.Id,
                        Title = "Application Status Test Internship",
                        Description = "Internship used for application status testing.",
                        RequiredSkills = "C#",
                        Location = "Dhaka",
                        Deadline = DateTime.Now.AddMonths(1),
                        Positions = 1,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    context.InternshipPostings.Add(statusInternship);
                    await context.SaveChangesAsync();
                }

                var applicationExists =
                    await context.InternshipApplications
                        .AnyAsync(a =>
                            a.InternshipPostingId == statusInternship.Id &&
                            a.StudentUserId == student.Id);

                if (!applicationExists)
                {
                    var application =
                        new InternshipApplication
                        {
                            InternshipPostingId = statusInternship.Id,
                            StudentUserId = student.Id,
                            Status = "Applied",
                            AppliedAt = DateTime.Now
                        };

                    context.InternshipApplications.Add(application);

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}