using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
namespace ABP.Infraestructure.Persistence.Context
{
    public class ArtemisBankDbContext(DbContextOptions<ArtemisBankDbContext> options) : DbContext(options)
    {
        public DbSet<Beneficiary> Beneficiaries { get; set; }
        public DbSet<Commerce> Commerces { get; set; }
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<CreditCardConsumption> Consumptions { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanInstallment> Installments { get; set; }
        public DbSet<SavingsAccount> Savings { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.HasDefaultSchema("artemisBank");
            mb.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
