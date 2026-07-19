namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class CommerceNodeEndpointConfiguration : IEntityTypeConfiguration<CommerceNodeEndpoint>
    {
        public void Configure(EntityTypeBuilder<CommerceNodeEndpoint> entity)
        {
            entity.ToTable(
                "commerce_node_endpoint",
                table => table.HasCheckConstraint(
                    "ck_commerce_node_endpoint_kind",
                    "kind in ('control_api', 'storefront', 'internal_api')"));

            entity.HasKey(endpoint => endpoint.Id);
            entity.Property(endpoint => endpoint.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(endpoint => endpoint.NodeId).HasColumnName("node_id");
            entity.Property(endpoint => endpoint.Kind).HasColumnName("kind").HasColumnType("text").IsRequired();
            entity.Property(endpoint => endpoint.Url).HasColumnName("url").HasColumnType("text").IsRequired();
            entity.Property(endpoint => endpoint.IsPrimary).HasColumnName("is_primary");
            entity.Property(endpoint => endpoint.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(endpoint => endpoint.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(endpoint => endpoint.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(endpoint => endpoint.NodeId);
            entity.HasIndex(endpoint => new { endpoint.NodeId, endpoint.Kind })
                .HasDatabaseName("commerce_node_endpoint_active_kind_idx")
                .HasFilter("disabled_at is null");
            entity.HasOne(endpoint => endpoint.Node)
                .WithMany(node => node.Endpoints)
                .HasForeignKey(endpoint => endpoint.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
