using ABP.Core.Application.Interfaces.IServices;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.identity.Entities;
using ABP.Infraestructure.identity.Services;
using ABP.Infraestructure.Shared.EmailServices;
using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Infraestructure.identity
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            GeneralConfiguration(services, config);
            return services;
        }
        #region private methods
        private static void GeneralConfiguration(IServiceCollection services, IConfiguration config)
        {
            #region Context
            if (config.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<IdentityContext>(opt => opt.UseInMemoryDatabase("AppDb"));
            }
            else
            {
                services.AddDbContext<IdentityContext>(options =>
                    options.UseMySQL(config.GetConnectionString("IdentityConnection")!));
            }
            #endregion

            #region identity configuration

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
            });

            #endregion

            #region IOC

            services.AddTransient<IUserReadOnlyService, UserReadOnlyService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IJwtService, JwtService>();
            services.AddTransient<ICorreoServices, EmailService>();
            services.AddTransient<IEmailServices, EmailService>();
            #endregion
        }
        #endregion
    }
}
