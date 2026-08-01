using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
    {
        public void Configure(EntityTypeBuilder<Beneficiary> builder)
        {
            builder.ToTable("Beneficiaries");
            builder.HasKey(b => b.Id);

            #region Properties
            builder.Property(b => b.AccountNumber).IsRequired().HasMaxLength(9);
            builder.Property(b => b.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(b => b.LastName).IsRequired().HasMaxLength(100);
            builder.Property(b => b.OwnerId).IsRequired();
            #endregion
            
            builder.HasIndex(b => new { b.OwnerId, b.AccountNumber }).IsUnique();
        }
    }
}
