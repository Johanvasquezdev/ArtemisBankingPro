using ABP.Core.Domain.Entities;
using ABP.Core.Application.Interfaces.IServices;
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
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("IdentitySeed");

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var commerceRepository = services.GetRequiredService<ICommerceRepository>();

            try
            {
                await DefaultRoles.SeedAsync(roleManager);
                await DefaultUsers.SeedAsync(userManager, null);
                logger.LogInformation("Roles y usuario administrador sembrados correctamente.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "FALLO sembrando roles/usuario admin. El login NO va a funcionar hasta corregir esto.");
                throw;
            }

            int? defaultCommerceId = null;
            try
            {
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

                defaultCommerceId = defaultCommerce.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo sembrar el comercio por defecto. Verifica que las migraciones de ArtemisBankDbContext esten aplicadas.");
            }

            try
            {
                await EnsureDefaultClientAccountAsync(userManager, services.GetRequiredService<ISavingsAccountService>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo sembrar la cuenta demo del cliente.");
            }
        }

        private static async Task EnsureDefaultClientAccountAsync(
            UserManager<ApplicationUser> userManager,
            ISavingsAccountService savingsAccountService)
        {
            var client = await userManager.FindByNameAsync("ClientUser");
            if (client is null || await savingsAccountService.HasActiveAccountAsync(client.Id))
                return;

            await savingsAccountService.CreateAccountAsync(client.Id, "SYSTEM-SEED", 10000m);
        }
    }
}
