namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class CommerceNodeConfiguration : IEntityTypeConfiguration<CommerceNode>
    {
        public void Configure(EntityTypeBuilder<CommerceNode> entity)
        {
            entity.ToTable(
                "commerce_node",
                table => table.HasCheckConstraint(
                    "ck_commerce_node_status",
                    "status in ('unknown', 'healthy', 'warning', 'down', 'disabled')"));

            entity.HasKey(node => node.Id);
            entity.Property(node => node.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(node => node.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(node => node.NodeKey).HasColumnName("node_key").HasColumnType("text").IsRequired();
            entity.Property(node => node.NodeSecret).HasColumnName("node_secret").HasColumnType("text");
            entity.Property(node => node.NodeSecretUpdatedAt).HasColumnName("node_secret_updated_at").HasColumnType("timestamp with time zone");
            entity.Property(node => node.Name).HasColumnName("name").HasColumnType("text").IsRequired();
            entity.Property(node => node.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(node => node.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(node => node.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamp with time zone");
            entity.Property(node => node.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(node => node.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(node => node.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(node => node.PublicId).IsUnique();
            entity.HasIndex(node => node.NodeKey)
                .IsUnique()
                .HasDatabaseName("commerce_node_active_node_key_uq")
                .HasFilter("disabled_at is null");
            entity.HasIndex(node => node.Status);
        }
    }
}
