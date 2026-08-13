using VebTur.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VebTur.Infrastructure.Persistence.Seed;

/// <summary>
/// Dev-only seeding of the "Admin" role and one admin account, sourced entirely from
/// configuration (User Secrets locally, environment variables in containers) — never a
/// hardcoded fallback password. If <c>Admin:Password</c> isn't configured, seeding is
/// skipped with a warning rather than creating an account with a guessable password.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminRoleName = "Admin";

    public static async Task SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            await roleManager.CreateAsync(new ApplicationRole(AdminRoleName));
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        var displayName = configuration["Admin:DisplayName"] ?? "Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Admin:Email/Admin:Password not configured — skipping dev admin account seeding. " +
                "Set them via User Secrets to enable admin panel login.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed dev admin account: {Errors}", errors);
            return;
        }

        await userManager.AddToRoleAsync(user, AdminRoleName);
    }
}
