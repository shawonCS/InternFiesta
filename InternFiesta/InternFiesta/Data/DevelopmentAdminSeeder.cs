
using InternFiesta.Models;
using Microsoft.AspNetCore.Identity;

namespace InternFiesta.Data
{
    // Development-only administrator setup.
    public static class DevelopmentAdminSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var userManager = services
                .GetRequiredService<UserManager<ApplicationUser>>();

            // If an Administrator already exists,
            // do not create another one.
            if ((await userManager.GetUsersInRoleAsync(
                    "Administrator")).Any())
            {
                return;
            }

            string? email =
                configuration["DemoAdmin:Email"]?.Trim();

            string? password =
                configuration["DemoAdmin:Password"];

            // Skip bootstrap if no credentials are configured.
            if (string.IsNullOrWhiteSpace(email) &&
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Both DemoAdmin:Email and " +
                    "DemoAdmin:Password are required.");
            }

            // Never promote an existing user automatically.
            if (await userManager.FindByEmailAsync(email) != null)
            {
                throw new InvalidOperationException(
                    "Demo admin email is already in use. " +
                    "Choose an unused email.");
            }

            var admin = new ApplicationUser
            {
                FullName = "Development Administrator",
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(
                admin, password);

            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create development administrator: " +
                    string.Join("; ",
                        created.Errors.Select(e => e.Description)));
            }

            var assigned = await userManager.AddToRoleAsync(
                admin, "Administrator");

            if (!assigned.Succeeded)
            {
                await userManager.DeleteAsync(admin);

                throw new InvalidOperationException(
                    "Unable to assign administrator role: " +
                    string.Join("; ",
                        assigned.Errors.Select(e => e.Description)));
            }
        }
    }
}
