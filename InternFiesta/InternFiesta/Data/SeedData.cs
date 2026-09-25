using InternFiesta.Models;
using Microsoft.AspNetCore.Identity;

namespace InternFiesta.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

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
        }
    }
}