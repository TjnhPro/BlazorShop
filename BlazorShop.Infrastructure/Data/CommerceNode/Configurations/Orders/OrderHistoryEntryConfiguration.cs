namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Orders
{
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class OrderHistoryEntryConfiguration : IEntityTypeConfiguration<OrderHistoryEntry>
    {
        public void Configure(EntityTypeBuilder<OrderHistoryEntry> entity)
        {
            entity.ToTable("order_history_entries");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).HasColumnName("id");
            entity.Property(entry => entry.StoreId).HasColumnName("store_id");
            entity.Property(entry => entry.OrderId).HasColumnName("order_id");
            entity.Property(entry => entry.EventType).HasColumnName("event_type").HasMaxLength(80).IsRequired();
            entity.Property(entry => entry.OldValue).HasColumnName("old_value").HasMaxLength(128);
            entity.Property(entry => entry.NewValue).HasColumnName("new_value").HasMaxLength(128);
            entity.Property(entry => entry.Message).HasColumnName("message").HasMaxLength(512).IsRequired();
            entity.Property(entry => entry.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(entry => entry.VisibleToCustomer).HasColumnName("visible_to_customer").HasDefaultValue(false);
            entity.Property(entry => entry.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(entry => entry.Source).HasColumnName("source").HasMaxLength(64).HasDefaultValue("system").IsRequired();

            entity.HasOne(entry => entry.Order)
                .WithMany()
                .HasForeignKey(entry => entry.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(entry => new { entry.StoreId, entry.OrderId, entry.CreatedAtUtc });
            entity.HasIndex(entry => new { entry.StoreId, entry.EventType, entry.CreatedAtUtc });
        }
    }
}
