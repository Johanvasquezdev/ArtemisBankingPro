using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ABP.Infraestructure.identity.Seeds
{
    public static class IdentitySeedExtensions
    {
        public static async Task SeedIdentityDataAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var commerceRepository = services.GetRequiredService<ICommerceRepository>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                var defaultCommerce = (await commerceRepository.GetAllAsync())
                    .FirstOrDefault(c => c.Name == "Default Commerce");

                if (defaultCommerce == null)
                {
                    defaultCommerce = new Commerce
                    {
                        Name = "Default Commerce",
                        Description = "Default seeded commerce",
                        Logo = "default-logo.png",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await commerceRepository.AddAsync(defaultCommerce);
                }

                await DefaultRoles.SeedAsync(roleManager);
                await DefaultUsers.SeedAsync(userManager, defaultCommerce.Id);
            }
            catch (Exception ex)
            {
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("IdentitySeed");
                logger.LogError(ex, "An error occurred during Identity seeding.");
            }
        }
    }
}
