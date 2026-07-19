namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class NodeCapabilitySnapshotConfiguration : IEntityTypeConfiguration<NodeCapabilitySnapshot>
    {
        public void Configure(EntityTypeBuilder<NodeCapabilitySnapshot> entity)
        {
            entity.ToTable("node_capability_snapshot");
            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(snapshot => snapshot.NodeId).HasColumnName("node_id");
            entity.Property(snapshot => snapshot.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(snapshot => snapshot.SchemaVersion).HasColumnName("schema_version").HasColumnType("text").IsRequired();
            entity.Property(snapshot => snapshot.Checksum).HasColumnName("checksum").HasColumnType("text").IsRequired();
            entity.Property(snapshot => snapshot.CapabilitiesJson).HasColumnName("capabilities_json").HasColumnType("jsonb").IsRequired();
            entity.Property(snapshot => snapshot.IsCurrent).HasColumnName("is_current");
            entity.Property(snapshot => snapshot.CapturedAt).HasColumnName("captured_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(snapshot => snapshot.PublicId).IsUnique();
            entity.HasIndex(snapshot => snapshot.NodeId);
            entity.HasIndex(snapshot => new { snapshot.NodeId, snapshot.IsCurrent })
                .HasDatabaseName("node_capability_snapshot_current_idx")
                .HasFilter("is_current");
            entity.HasIndex(snapshot => new { snapshot.NodeId, snapshot.Checksum });
            entity.HasOne(snapshot => snapshot.Node)
                .WithMany(node => node.CapabilitySnapshots)
                .HasForeignKey(snapshot => snapshot.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
