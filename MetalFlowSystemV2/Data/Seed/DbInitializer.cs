using MetalFlowSystemV2.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Seed;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure DB is migrated
        await context.Database.MigrateAsync();

        // Roles
        string[] roleNames = { "Admin", "Planner", "Supervisor", "Operator", "Driver" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Admin User
        var adminEmail = "admin@metalflow.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Admin"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Branches
        if (!await context.Branches.AnyAsync())
        {
            var branches = new List<Branch>
            {
                // Canada
                new Branch { Code = "DEL", Name = "Delta", City = "Delta", Region = "BC", Country = "Canada" },
                new Branch { Code = "SUR", Name = "Surrey", City = "Surrey", Region = "BC", Country = "Canada" },
                new Branch { Code = "CAL", Name = "Calgary", City = "Calgary", Region = "AB", Country = "Canada" },
                new Branch { Code = "EDM", Name = "Edmonton", City = "Edmonton", Region = "AB", Country = "Canada" },
                new Branch { Code = "SAS", Name = "Saskatoon", City = "Saskatoon", Region = "SK", Country = "Canada" },
                new Branch { Code = "BRD", Name = "Brandon", City = "Brandon", Region = "MB", Country = "Canada" },
                new Branch { Code = "WPG", Name = "Winnipeg", City = "Winnipeg", Region = "MB", Country = "Canada" },
                new Branch { Code = "HAM", Name = "Hamilton", City = "Hamilton", Region = "ON", Country = "Canada" },
                new Branch { Code = "DOR", Name = "Dorval", City = "Dorval", Region = "QC", Country = "Canada" },
                new Branch { Code = "MIS", Name = "Mississauga", City = "Mississauga", Region = "ON", Country = "Canada" },

                // USA
                new Branch { Code = "KNT", Name = "Kent", City = "Kent", Region = "WA", Country = "USA" },
                new Branch { Code = "LON", Name = "Longview", City = "Longview", Region = "WA", Country = "USA" },
                new Branch { Code = "PAS", Name = "Pasco", City = "Pasco", Region = "WA", Country = "USA" },
                new Branch { Code = "SPO", Name = "Spokane", City = "Spokane", Region = "WA", Country = "USA" },
                new Branch { Code = "HUB", Name = "Hubbard", City = "Hubbard", Region = "OR", Country = "USA" },
                new Branch { Code = "NAM", Name = "Nampa", City = "Nampa", Region = "ID", Country = "USA" }
            };

            await context.Branches.AddRangeAsync(branches);
            await context.SaveChangesAsync();
        }
    }
}
