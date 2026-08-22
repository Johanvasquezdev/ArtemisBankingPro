using ABP.Core.Application.Behaviors;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Application.Mappings;
using ABP.Core.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            services.AddScoped<TransactionQueryService>();
            services.AddScoped<TransactionOperationDependencies>(sp => new TransactionOperationDependencies(
                sp.GetRequiredService<ITransactionRepository>(),
                sp.GetRequiredService<ISavingsAccountRepository>(),
                sp.GetRequiredService<IUserReadOnlyService>(),
                sp.GetRequiredService<IEmailServices>(),
                sp.GetRequiredService<ICreditCardRepository>(),
                sp.GetRequiredService<ILoanRepository>(),
                sp.GetRequiredService<ILoanInstallmentRepository>(),
                sp.GetRequiredService<IBeneficiaryRepository>(),
                sp.GetRequiredService<ICreditCardConsumptionRepository>(),
                sp.GetRequiredService<ITransactionRecorder>(),
                sp.GetRequiredService<IOverpaymentCalculator>(),
                sp.GetRequiredService<ILoanPaymentAllocationService>(),
                sp.GetRequiredService<IDateTimeProvider>(),
                sp.GetRequiredService<IUnitOfWork>(),
                sp.GetRequiredService<ILogger<TransactionService>>(),
                sp.GetService<IIdempotencyRepository>()));
            services.AddScoped<ClientTransactionService>();
            services.AddScoped<CashierTransactionService>();
            services.AddScoped<TransactionService>(sp => new TransactionService(
                sp.GetRequiredService<ITransactionQueryService>(),
                sp.GetRequiredService<IClientTransactionService>(),
                sp.GetRequiredService<ICashierTransactionService>()));
            services.AddScoped<ITransactionService>(sp => sp.GetRequiredService<TransactionService>());
            services.AddScoped<ITransactionQueryService>(sp => sp.GetRequiredService<TransactionQueryService>());
            services.AddScoped<IClientTransactionService>(sp => sp.GetRequiredService<ClientTransactionService>());
            services.AddScoped<ICashierTransactionService>(sp => sp.GetRequiredService<CashierTransactionService>());
            services.AddTransient<IBeneficiaryService, BeneficiaryService>();
            services.AddTransient<ICommerceService, CommerceService>();
            services.AddTransient<ICreditCardConsumptionService, CreditCardConsumptionService>();
            services.AddTransient<ILoanInstallmentService, LoanInstallmentService>();
            services.AddTransient<IDashboardService, DashboardService>();
            services.AddTransient<IPaymentProcessorService, PaymentProcessorService>();
            services.AddTransient<IVirtualCardService, VirtualCardService>();

            services.AddScoped<ITransactionRecorder, TransactionRecorder>();
            services.AddScoped<IOverpaymentCalculator, AntiOverpaymentCalculator>();
            services.AddScoped<ILoanPaymentAllocationService, LoanPaymentAllocationService>();

            // CQRS + MediatR + FluentValidation pipeline
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining(typeof(ServiceRegistration));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssemblyContaining(typeof(ServiceRegistration));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            return services;
        }
    }
}
