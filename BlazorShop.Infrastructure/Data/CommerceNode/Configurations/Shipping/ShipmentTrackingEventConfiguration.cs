namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Shipping
{
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ShipmentTrackingEventConfiguration : IEntityTypeConfiguration<ShipmentTrackingEvent>
    {
        public void Configure(EntityTypeBuilder<ShipmentTrackingEvent> entity)
        {
            entity.ToTable("ShipmentTrackingEvents");
            entity.HasKey(trackingEvent => trackingEvent.Id);

            entity.Property(trackingEvent => trackingEvent.Status)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(trackingEvent => trackingEvent.Message)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(trackingEvent => trackingEvent.Location)
                .HasMaxLength(160);

            entity.Property(trackingEvent => trackingEvent.Source)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(trackingEvent => trackingEvent.OccurredAtUtc)
                .HasColumnType("timestamp with time zone");

            entity.Property(trackingEvent => trackingEvent.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(trackingEvent => new { trackingEvent.StoreId, trackingEvent.OrderId, trackingEvent.OccurredAtUtc });
            entity.HasIndex(trackingEvent => new { trackingEvent.ShipmentId, trackingEvent.OccurredAtUtc });
        }
    }
}
