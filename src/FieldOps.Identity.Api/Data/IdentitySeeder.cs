using FieldOps.Identity.Api.Authorization;
using FieldOps.Identity.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace FieldOps.Identity.Api.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var roleName in FieldOpsRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName
                    });
                }
            }

            var dispatcher = await userManager.FindByEmailAsync("dispatcher@fieldops.com");
            if (dispatcher is not null && !await userManager.IsInRoleAsync(dispatcher, FieldOpsRoles.Dispatcher))
            {
                await userManager.AddToRoleAsync(dispatcher, FieldOpsRoles.Dispatcher);
            }
        }
    }
}
