using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    internal class CommerceConfiguration : IEntityTypeConfiguration<Commerce>
    {
        public void Configure(EntityTypeBuilder<Commerce> builder)
        {
            builder.ToTable("Commerces");
            builder.HasKey(c => c.Id);

            #region Properties
            builder.Property(c => c.Logo).IsRequired().HasMaxLength(500);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Description).IsRequired().HasMaxLength(500);
            builder.Property(c => c.Rnc).IsRequired().HasMaxLength(9);
            builder.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
            builder.Property(c => c.CreatedByAdminId).IsRequired(false).HasMaxLength(450);
            builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
            builder.Property(c => c.CreatedAt).IsRequired();

            #endregion

            #region indexes
            builder.HasIndex(c => c.Rnc).IsUnique();
            builder.HasIndex(c => c.Email).IsUnique();
            #endregion
        }
    }
}
