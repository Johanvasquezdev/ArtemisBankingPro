using ABP.Core.Domain.Interfaces;
using ABP.Core.Domain.Interfaces.IGenerics;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Infraestructure.Persistence.IoC
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            GeneralConfiguration(services, config);

        }
        #region private methods
        private static void GeneralConfiguration(IServiceCollection services, IConfiguration config)
        {
            #region Context
            if (config.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<ArtemisBankDbContext>(opt => opt.UseInMemoryDatabase("AppDb"));
            }
            else
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                }
                services.AddDbContext<ArtemisBankDbContext>(
                    (servicesProvider, opt) =>
                    {
                        opt.EnableSensitiveDataLogging();
                        opt.UseNpgsql(connectionString,
                            m => m.MigrationsAssembly(typeof(ArtemisBankDbContext).Assembly.FullName));
                    },
                    contextLifetime: ServiceLifetime.Scoped,
                    optionsLifetime: ServiceLifetime.Scoped
                );
            }
            #endregion

            #region IOC
            // Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();
            services.AddScoped<ICommerceRepository, CommerceRepository>();
            services.AddScoped<ICreditCardConsumptionRepository, CreditCardConsumptionRepository>();
            services.AddScoped<ICreditCardRepository, CreditCardRepository>();
            services.AddScoped<ILoanRepository, LoanRepository>();
            services.AddScoped<ILoanInstallmentRepository, LoanInstallmentRepository>();
            services.AddScoped<ISavingsAccountRepository, SavingsAccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            #endregion
        }
        #endregion
    
    }
}
