namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class StoreRegistryConfiguration : IEntityTypeConfiguration<StoreRegistry>
    {
        public void Configure(EntityTypeBuilder<StoreRegistry> entity)
        {
            entity.ToTable(
                "store_registry",
                table => table.HasCheckConstraint(
                    "ck_store_registry_status",
                    "status in ('active', 'provisioning', 'disabled', 'archived')"));

            entity.HasKey(store => store.Id);
            entity.Property(store => store.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(store => store.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(store => store.NodeId).HasColumnName("node_id");
            entity.Property(store => store.StoreKey).HasColumnName("store_key").HasColumnType("text").IsRequired();
            entity.Property(store => store.Name).HasColumnName("name").HasColumnType("text").IsRequired();
            entity.Property(store => store.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(store => store.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(store => store.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(store => store.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(store => store.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(store => store.PublicId).IsUnique();
            entity.HasIndex(store => store.NodeId);
            entity.HasIndex(store => new { store.NodeId, store.StoreKey })
                .IsUnique()
                .HasDatabaseName("store_registry_active_node_store_key_uq")
                .HasFilter("archived_at is null");
            entity.HasOne(store => store.Node)
                .WithMany(node => node.Stores)
                .HasForeignKey(store => store.NodeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
