using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.ToTable("Loans");
            builder.HasKey(l => l.Id);

            #region properties
            builder.Property(l => l.ClientId).IsRequired().HasMaxLength(450);
            builder.Property(l => l.AssignedByAdminId).IsRequired().HasMaxLength(450);

            builder.Property(l => l.LoanNumber).IsRequired().HasMaxLength(9);
            builder.Property(l => l.Amount).IsRequired().HasPrecision(18,2);
            builder.Property(l => l.AnualInterestRate).IsRequired().HasPrecision(8,4);
            builder.Property(l => l.TermInMonths).IsRequired();
            builder.Property(l => l.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(l => l.CreatedAt).IsRequired();
            #endregion

            #region relationships
            builder.HasMany(l => l.Installments)
                   .WithOne() 
                   .HasForeignKey(li => li.LoanId)
                   .OnDelete(DeleteBehavior.Cascade);
            #endregion

            builder.HasIndex(l => l.LoanNumber).IsUnique();
        }
    }
}
