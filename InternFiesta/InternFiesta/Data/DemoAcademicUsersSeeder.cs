
using InternFiesta.Models;
using Microsoft.AspNetCore.Identity;

namespace InternFiesta.Data
{
    public static class DemoAcademicUsersSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            await CreateDemoUserAsync(
                userManager,
                roleManager,
                "UniversityCoordinator",
                "coordinator@internfiesta.test",
                "University Coordinator",
                configuration["DemoAccounts:CoordinatorPassword"]);

            await CreateDemoUserAsync(
                userManager,
                roleManager,
                "FacultySupervisor",
                "faculty@internfiesta.test",
                "Faculty Supervisor",
                configuration["DemoAccounts:FacultyPassword"]);
        }

        private static async Task CreateDemoUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            string roleName,
            string email,
            string fullName,
            string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Demo password is missing for {roleName}.");
            }

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ",
                            roleResult.Errors.Select(e => e.Description)));
                }
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    user, password);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Cannot create {email}: " +
                        string.Join("; ",
                            result.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                var result = await userManager.AddToRoleAsync(
                    user, roleName);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
