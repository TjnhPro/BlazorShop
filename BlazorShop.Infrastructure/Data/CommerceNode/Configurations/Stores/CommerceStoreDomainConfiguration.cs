namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Stores
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceStoreDomainConfiguration : IEntityTypeConfiguration<CommerceStoreDomain>
    {
        public void Configure(EntityTypeBuilder<CommerceStoreDomain> entity)
        {
            entity.ToTable("commerce_store_domain");
            entity.HasKey(domain => domain.Id);
            entity.Property(domain => domain.Id).HasColumnName("id");
            entity.Property(domain => domain.StoreId).HasColumnName("store_id");
            entity.Property(domain => domain.Domain).HasColumnName("domain").IsRequired();
            entity.Property(domain => domain.NormalizedDomain).HasColumnName("normalized_domain").IsRequired();
            entity.Property(domain => domain.IsPrimary).HasColumnName("is_primary");
            entity.Property(domain => domain.Status).HasColumnName("status").IsRequired();
            entity.Property(domain => domain.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(domain => domain.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(domain => domain.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
            entity.Property(domain => domain.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");

            entity.HasOne(domain => domain.Store)
                .WithMany(store => store.Domains)
                .HasForeignKey(domain => domain.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(domain => domain.StoreId);
            entity.HasIndex(domain => domain.NormalizedDomain)
                .IsUnique()
                .HasFilter("disabled_at IS NULL");
            entity.HasIndex(domain => new { domain.StoreId, domain.IsPrimary })
                .IsUnique()
                .HasFilter("is_primary = true AND disabled_at IS NULL");

            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_commerce_store_domain_status",
                    "status in ('pending', 'verified', 'disabled')"));
        }
    }
}
