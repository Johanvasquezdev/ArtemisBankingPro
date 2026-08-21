using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Shared.EmailServices;
using ABP.Infraestructure.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Infraestructure.Shared.IoC
{
    public static class ServiceRegistration
    {
        public static void AddSharedInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddTransient<IEmailServices, EmailService>();
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            
            services.AddTransient<IPaymentGatewayService, StripePaymentService>();
            services.AddTransient<ISmsService, TwilioSmsService>();
            services.AddTransient<IOcrService, TesseractOcrService>();
        }
    }
}
