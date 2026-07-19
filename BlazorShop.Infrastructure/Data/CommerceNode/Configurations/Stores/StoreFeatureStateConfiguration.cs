namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Stores
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreFeatureStateConfiguration : IEntityTypeConfiguration<StoreFeatureState>
    {
        public void Configure(EntityTypeBuilder<StoreFeatureState> entity)
        {
            entity.ToTable("store_feature_states");
            entity.HasKey(feature => feature.Id);
            entity.Property(feature => feature.Id).HasColumnName("id");
            entity.Property(feature => feature.StoreId).HasColumnName("store_id");
            entity.Property(feature => feature.FeatureKey).HasColumnName("feature_key").HasMaxLength(64).IsRequired();
            entity.Property(feature => feature.Enabled).HasColumnName("enabled");
            entity.Property(feature => feature.Reason).HasColumnName("reason").HasMaxLength(500);
            entity.Property(feature => feature.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(feature => feature.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(feature => new { feature.StoreId, feature.FeatureKey }).IsUnique();
            entity.HasIndex(feature => new { feature.StoreId, feature.Enabled });

            entity.HasOne(feature => feature.Store)
                .WithMany()
                .HasForeignKey(feature => feature.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                "store_feature_states",
                table => table.HasCheckConstraint(
                    "ck_store_feature_states_feature_key",
                    "feature_key in ('checkout', 'customerAccounts', 'newsletter', 'recommendations', 'reviews')"));
        }
    }
}
