using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class Creditcardconsumptionconfiguration : IEntityTypeConfiguration<CreditCardConsumption>
    {
        public void Configure(EntityTypeBuilder<CreditCardConsumption> builder)
        {
            builder.ToTable("CreditCardConsumptions");
            builder.HasKey(c => c.Id);

            #region properties
            builder.Property(c => c.TransactionDate).IsRequired();
            builder.Property(c => c.CommerceName).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.Amount).IsRequired().HasPrecision(18, 2);
            #endregion

            #region relationships
            builder.HasOne(c => c.CreditCard)
                .WithMany(cc => cc.Consumptions)
                .HasForeignKey(c => c.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Commerce)
                .WithMany(co => co.Consumptions)
                .HasForeignKey(c => c.CommerceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            #endregion
        }
    }
}
