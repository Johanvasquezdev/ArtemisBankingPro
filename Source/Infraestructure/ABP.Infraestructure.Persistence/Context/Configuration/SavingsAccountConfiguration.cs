using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class SavingsAccountConfiguration : IEntityTypeConfiguration<SavingsAccount>
    {
        public void Configure(EntityTypeBuilder<SavingsAccount> builder)
        {
            builder.ToTable("SavingsAccounts");
            builder.HasKey(s => s.Id);

            #region properties
            builder.Property(s => s.UserId).IsRequired().HasMaxLength(450);
            builder.Property(s => s.CreatedByAdminId).IsRequired().HasMaxLength(450);
            builder.Property(s => s.AccountNumber).IsRequired().HasMaxLength(9);
            builder.Property(s => s.Balance).IsRequired().HasPrecision(18,2);
            builder.Property(s => s.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.CreatedAt).IsRequired();
            #endregion

            builder.HasIndex(s => s.AccountNumber).IsUnique();
        }
    }
}
