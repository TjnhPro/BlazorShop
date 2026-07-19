namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.SecurityPrivacy
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StorefrontConsentEventConfiguration : IEntityTypeConfiguration<StorefrontConsentEvent>
    {
        public void Configure(EntityTypeBuilder<StorefrontConsentEvent> entity)
        {
            entity.ToTable("storefront_consent_event");
            entity.HasKey(consentEvent => consentEvent.Id);
            entity.Property(consentEvent => consentEvent.Id).HasColumnName("id");
            entity.Property(consentEvent => consentEvent.StoreId).HasColumnName("store_id");
            entity.Property(consentEvent => consentEvent.ConsentKey).HasColumnName("consent_key").HasMaxLength(64).IsRequired();
            entity.Property(consentEvent => consentEvent.EventType).HasColumnName("event_type").HasMaxLength(32).IsRequired();
            entity.Property(consentEvent => consentEvent.ConsentVersion).HasColumnName("consent_version").HasMaxLength(64).IsRequired();
            entity.Property(consentEvent => consentEvent.CategoriesJson).HasColumnName("categories_json").HasColumnType("jsonb").IsRequired();
            entity.Property(consentEvent => consentEvent.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(consentEvent => consentEvent.Store)
                .WithMany()
                .HasForeignKey(consentEvent => consentEvent.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(consentEvent => new { consentEvent.StoreId, consentEvent.ConsentKey });
            entity.HasIndex(consentEvent => consentEvent.OccurredAtUtc);
        }
    }
}
