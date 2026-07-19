namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Shipping
{
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
    {
        public void Configure(EntityTypeBuilder<ShipmentItem> entity)
        {
            entity.ToTable("ShipmentItems");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Quantity)
                .IsRequired();

            entity.Property(item => item.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(item => item.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(item => item.ShipmentId);
            entity.HasIndex(item => item.OrderLineId);
            entity.HasIndex(item => new { item.ShipmentId, item.OrderLineId })
                .IsUnique();

            entity.HasOne(item => item.OrderLine)
                .WithMany()
                .HasForeignKey(item => item.OrderLineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
