namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class CommerceNodeCredentialConfiguration : IEntityTypeConfiguration<CommerceNodeCredential>
    {
        public void Configure(EntityTypeBuilder<CommerceNodeCredential> entity)
        {
            entity.ToTable(
                "commerce_node_credential",
                table => table.HasCheckConstraint(
                    "ck_commerce_node_credential_status",
                    "status in ('active', 'revoked', 'rotated')"));

            entity.HasKey(credential => credential.Id);
            entity.Property(credential => credential.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(credential => credential.NodeId).HasColumnName("node_id");
            entity.Property(credential => credential.KeyId).HasColumnName("key_id").HasColumnType("text").IsRequired();
            entity.Property(credential => credential.SecretHash).HasColumnName("secret_hash").HasColumnType("text").IsRequired();
            entity.Property(credential => credential.HashAlgorithm).HasColumnName("hash_algorithm").HasColumnType("text").IsRequired();
            entity.Property(credential => credential.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(credential => credential.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(credential => credential.RevealedAt).HasColumnName("revealed_at").HasColumnType("timestamp with time zone");
            entity.Property(credential => credential.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
            entity.Property(credential => credential.CreatedByAdminUserId).HasColumnName("created_by_admin_user_id");
            entity.Property(credential => credential.RevokedByAdminUserId).HasColumnName("revoked_by_admin_user_id");
            entity.HasIndex(credential => credential.NodeId);
            entity.HasIndex(credential => credential.CreatedByAdminUserId);
            entity.HasIndex(credential => credential.RevokedByAdminUserId);
            entity.HasIndex(credential => credential.KeyId).IsUnique();
            entity.HasIndex(credential => new { credential.NodeId, credential.Status })
                .HasDatabaseName("commerce_node_credential_active_node_idx")
                .HasFilter("revoked_at is null");
            entity.HasOne(credential => credential.Node)
                .WithMany(node => node.Credentials)
                .HasForeignKey(credential => credential.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(credential => credential.CreatedByAdminUser)
                .WithMany()
                .HasForeignKey(credential => credential.CreatedByAdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(credential => credential.RevokedByAdminUser)
                .WithMany()
                .HasForeignKey(credential => credential.RevokedByAdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
