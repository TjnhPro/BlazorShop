namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Shipping
{
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
    {
        public void Configure(EntityTypeBuilder<Shipment> entity)
        {
            entity.ToTable("Shipments");
            entity.HasKey(shipment => shipment.Id);

            entity.Property(shipment => shipment.ShipDate)
                .HasColumnType("timestamp with time zone");

            entity.Property(shipment => shipment.CarrierName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(shipment => shipment.CarrierService)
                .HasMaxLength(128);

            entity.Property(shipment => shipment.TrackingNumber)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(shipment => shipment.TrackingUrl)
                .HasMaxLength(1024);

            entity.Property(shipment => shipment.Note)
                .HasMaxLength(1000);

            entity.Property(shipment => shipment.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(shipment => shipment.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(shipment => new { shipment.StoreId, shipment.OrderId })
                .IsUnique();

            entity.HasIndex(shipment => shipment.StoreId);
            entity.HasIndex(shipment => shipment.OrderId);

            entity.HasOne(shipment => shipment.Order)
                .WithMany()
                .HasForeignKey(shipment => shipment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(shipment => shipment.Items)
                .WithOne(item => item.Shipment)
                .HasForeignKey(item => item.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(shipment => shipment.TrackingEvents)
                .WithOne(trackingEvent => trackingEvent.Shipment)
                .HasForeignKey(trackingEvent => trackingEvent.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
