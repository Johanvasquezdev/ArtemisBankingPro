using ABP.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ABP.Infraestructure.Persistence.Context.Configuration
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Operation).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(200);
            builder.Property(x => x.ActorUserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.Operation, x.Key, x.ActorUserId }).IsUnique();
        }
    }
}
