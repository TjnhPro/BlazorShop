namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.SecurityPrivacy
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StorefrontConsentStateConfiguration : IEntityTypeConfiguration<StorefrontConsentState>
    {
        public void Configure(EntityTypeBuilder<StorefrontConsentState> entity)
        {
            entity.ToTable("storefront_consent_state");
            entity.HasKey(consent => consent.Id);
            entity.Property(consent => consent.Id).HasColumnName("id");
            entity.Property(consent => consent.StoreId).HasColumnName("store_id");
            entity.Property(consent => consent.ConsentKey).HasColumnName("consent_key").HasMaxLength(64).IsRequired();
            entity.Property(consent => consent.VisitorKeyHash).HasColumnName("visitor_key_hash").HasMaxLength(64).IsRequired();
            entity.Property(consent => consent.ConsentVersion).HasColumnName("consent_version").HasMaxLength(64).IsRequired();
            entity.Property(consent => consent.EssentialAccepted).HasColumnName("essential_accepted").HasDefaultValue(true);
            entity.Property(consent => consent.PreferencesAccepted).HasColumnName("preferences_accepted");
            entity.Property(consent => consent.AnalyticsAccepted).HasColumnName("analytics_accepted");
            entity.Property(consent => consent.MarketingAccepted).HasColumnName("marketing_accepted");
            entity.Property(consent => consent.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(consent => consent.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(consent => consent.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(consent => consent.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");

            entity.HasOne(consent => consent.Store)
                .WithMany()
                .HasForeignKey(consent => consent.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(consent => consent.ConsentKey).IsUnique();
            entity.HasIndex(consent => new { consent.StoreId, consent.VisitorKeyHash, consent.ConsentVersion }).IsUnique();
            entity.HasIndex(consent => consent.ExpiresAtUtc);
        }
    }
}
