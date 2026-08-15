using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
    {
        public void Configure(EntityTypeBuilder<LoanInstallment> builder)
        {
            builder.ToTable("LoanInstallments");
            builder.HasKey(li => li.Id);

            #region properties
            builder.Property(li => li.DueDate).IsRequired();
            builder.Property(li => li.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(li => li.InstallmentAmount).IsRequired().HasPrecision(18,2);
            builder.Property(li => li.InstallmentNumber).IsRequired();
            builder.Property(li => li.AmountPaid).IsRequired().HasPrecision(18,2).HasDefaultValue(0m);
            builder.Property(li => li.PrincipalPortion).IsRequired().HasPrecision(18,2).HasDefaultValue(0m);
            builder.Property(li => li.InterestPortion).IsRequired().HasPrecision(18,2).HasDefaultValue(0m);
            builder.Property(li => li.IsOverdue).IsRequired().HasDefaultValue(false);
            #endregion

            #region relationships
            builder.HasOne(li => li.Loan)
                .WithMany(l => l.Installments)
                .HasForeignKey(li => li.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}
