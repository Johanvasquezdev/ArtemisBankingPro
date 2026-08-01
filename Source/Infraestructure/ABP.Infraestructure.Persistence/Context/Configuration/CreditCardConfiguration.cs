using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
    {
        public void Configure(EntityTypeBuilder<CreditCard> builder)
        {
            builder.ToTable("CreditCards");
            builder.HasKey(cc => cc.Id);

            #region properties
            builder.Property(cc => cc.CardNumber).IsRequired().HasMaxLength(16);
            builder.Property(cc => cc.CreditLimit).IsRequired().HasPrecision(18,2);
            builder.Property(cc => cc.ExpirationDate).IsRequired().HasMaxLength(5);
            builder.Property(cc => cc.AmountOwed).IsRequired().HasPrecision(18,2).HasDefaultValue(0m);
            builder.Property(cc => cc.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(cc => cc.CVCHash).IsRequired().HasMaxLength(64);
            builder.Property(cc => cc.CreatedAt).IsRequired();

            builder.Property(cc => cc.ClientId).IsRequired().HasMaxLength(450);
            builder.Property(cc => cc.AssignedByAdminId).IsRequired().HasMaxLength(450);
            #endregion
            
            builder.HasIndex(cc => cc.CardNumber).IsUnique();
        }
    }
}
