namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class NodeHealthSnapshotConfiguration : IEntityTypeConfiguration<NodeHealthSnapshot>
    {
        public void Configure(EntityTypeBuilder<NodeHealthSnapshot> entity)
        {
            entity.ToTable(
                "node_health_snapshot",
                table => table.HasCheckConstraint(
                    "ck_node_health_snapshot_status",
                    "status in ('healthy', 'warning', 'down', 'timeout', 'malformed', 'unknown')"));

            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(snapshot => snapshot.NodeId).HasColumnName("node_id");
            entity.Property(snapshot => snapshot.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(snapshot => snapshot.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(snapshot => snapshot.HttpStatusCode).HasColumnName("http_status_code");
            entity.Property(snapshot => snapshot.DurationMs).HasColumnName("duration_ms");
            entity.Property(snapshot => snapshot.DependencyStatusJson).HasColumnName("dependency_status_json").HasColumnType("jsonb");
            entity.Property(snapshot => snapshot.ErrorCode).HasColumnName("error_code").HasColumnType("text");
            entity.Property(snapshot => snapshot.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
            entity.Property(snapshot => snapshot.CheckedAt).HasColumnName("checked_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(snapshot => snapshot.PublicId).IsUnique();
            entity.HasIndex(snapshot => snapshot.NodeId);
            entity.HasIndex(snapshot => new { snapshot.NodeId, snapshot.CheckedAt });
            entity.HasIndex(snapshot => new { snapshot.Status, snapshot.CheckedAt });
            entity.HasOne(snapshot => snapshot.Node)
                .WithMany(node => node.HealthSnapshots)
                .HasForeignKey(snapshot => snapshot.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
