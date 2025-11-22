using Domain.Common.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Outbox
{
    internal class OutboxEntityConfigurations : IEntityTypeConfiguration<OutboxEntity>
    {
        public void Configure(EntityTypeBuilder<OutboxEntity> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Type).IsRequired().HasMaxLength(100);
            builder.Property(o => o.Payload).IsRequired();
            builder.Property(o => o.RetryCount).IsRequired();
            builder.Property(o => o.Status).HasMaxLength(100).IsRequired();
            builder.Property(o => o.Error).HasMaxLength(1000);
            builder.Property(o => o.CreatedAtUtc).IsRequired();
            builder.Property(o => o.LastUpdatedAtUtc).IsRequired();
            builder.Property(o => o.ExpiresAtUtc).IsRequired();
        }
    }
}
