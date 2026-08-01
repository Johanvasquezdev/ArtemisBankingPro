using ABP.Infraestructure.identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.identity.Context.Configuration
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(au => au.Id);
            builder.HasIndex(au => au.Cedula).IsUnique();

            #region properties
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Cedula).IsRequired().HasMaxLength(12);
            builder.Property(u => u.Role).IsRequired().HasConversion<string>();
            builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(false);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.Property(u => u.ActivationToken).IsRequired(false);
            builder.Property(u => u.CommerceId).IsRequired(false);
            #endregion

        }
    }
}
