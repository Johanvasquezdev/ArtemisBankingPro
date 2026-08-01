using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Application.Mappings;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Core.Application.IoC
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

            services.AddTransient<ISavingsAccountService, SavingsAccountService>();
            services.AddTransient<ICreditCardService, CreditCardService>();
            services.AddTransient<ILoanService, LoanService>();
            services.AddTransient<ITransactionService, TransactionService>();
            services.AddTransient<IBeneficiaryService, BeneficiaryService>();
            services.AddTransient<ICommerceService, CommerceService>();
            services.AddTransient<ICreditCardConsumptionService, CreditCardConsumptionService>();
            services.AddTransient<ILoanInstallmentService, LoanInstallmentService>();
            services.AddTransient<IDashboardService, DashboardService>();
            services.AddTransient<IPaymentProcessorService, PaymentProcessorService>();

            return services;
        }
    }
}
