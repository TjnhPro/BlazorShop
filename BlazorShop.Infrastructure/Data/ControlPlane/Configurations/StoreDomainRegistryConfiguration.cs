namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class StoreDomainRegistryConfiguration : IEntityTypeConfiguration<StoreDomainRegistry>
    {
        public void Configure(EntityTypeBuilder<StoreDomainRegistry> entity)
        {
            entity.ToTable(
                "store_domain_registry",
                table => table.HasCheckConstraint(
                    "ck_store_domain_registry_status",
                    "status in ('pending', 'verified', 'disabled')"));

            entity.HasKey(domain => domain.Id);
            entity.Property(domain => domain.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(domain => domain.StoreId).HasColumnName("store_id");
            entity.Property(domain => domain.Domain).HasColumnName("domain").HasColumnType("text").IsRequired();
            entity.Property(domain => domain.NormalizedDomain).HasColumnName("normalized_domain").HasColumnType("text").IsRequired();
            entity.Property(domain => domain.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(domain => domain.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(domain => domain.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(domain => domain.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
            entity.Property(domain => domain.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(domain => domain.StoreId);
            entity.HasIndex(domain => domain.NormalizedDomain)
                .IsUnique()
                .HasDatabaseName("store_domain_registry_active_domain_uq")
                .HasFilter("disabled_at is null");
            entity.HasOne(domain => domain.Store)
                .WithMany(store => store.Domains)
                .HasForeignKey(domain => domain.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
