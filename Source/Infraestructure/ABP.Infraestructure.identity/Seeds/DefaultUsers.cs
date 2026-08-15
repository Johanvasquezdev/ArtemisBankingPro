using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace ABP.Infraestructure.identity.Seeds
{
    public static class DefaultUsers
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, int? defaultCommerceId = null)
        {
            await SeedUserAsync(userManager, new ApplicationUser
            {
                UserName = "AdminUser",
                Email = "admin@artemisbank.com",
                FirstName = "Admin",
                LastName = "ArtemisBank",
                Cedula = "00000000001",
                EmailConfirmed = true,
                IsActive = true,
                Role = UserRole.Admin
            }, "123Pa$$word!", UserRole.Admin);

            await SeedUserAsync(userManager, new ApplicationUser
            {
                UserName = "CashierUser",
                Email = "cashier@artemisbank.com",
                FirstName = "Cashier",
                LastName = "ArtemisBank",
                Cedula = "00000000002",
                EmailConfirmed = true,
                IsActive = true,
                Role = UserRole.Cashier
            }, "123Pa$$word!", UserRole.Cashier);

            await SeedUserAsync(userManager, new ApplicationUser
            {
                UserName = "ClientUser",
                Email = "client@artemisbank.com",
                FirstName = "Client",
                LastName = "ArtemisBank",
                Cedula = "00000000003",
                EmailConfirmed = true,
                IsActive = true,
                Role = UserRole.Client
            }, "123Pa$$word!", UserRole.Client);
        }

        private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string password, UserRole role)
        {
            if (await userManager.FindByEmailAsync(user.Email!) == null)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role.ToString());
                }
            }
        }
    }
}