using System.Linq;
using MagdyPOS.Authorization;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagdyPOS.Data;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                var r = await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
                if (!r.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(",", r.Errors.Select(e => e.Description)));
                }
            }
        }

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Organization_Profile (
                Id INTEGER NOT NULL CONSTRAINT PK_Organization_Profile PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Address TEXT NOT NULL
            );
            """).ConfigureAwait(false);

        _ = configuration;
        _ = userManager;
    }
}
