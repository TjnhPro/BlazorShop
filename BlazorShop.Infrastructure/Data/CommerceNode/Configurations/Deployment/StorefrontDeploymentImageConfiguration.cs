namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Deployment
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StorefrontDeploymentImageConfiguration : IEntityTypeConfiguration<StorefrontDeploymentImage>
    {
        public void Configure(EntityTypeBuilder<StorefrontDeploymentImage> entity)
        {
            entity.ToTable("storefront_deployment_image");
            entity.HasKey(image => image.Id);
            entity.Property(image => image.Id).HasColumnName("id");
            entity.Property(image => image.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
            entity.Property(image => image.Image).HasColumnName("image").HasMaxLength(500).IsRequired();
            entity.Property(image => image.Version).HasColumnName("version").HasMaxLength(100);
            entity.Property(image => image.IsDefault).HasColumnName("is_default");
            entity.Property(image => image.IsEnabled).HasColumnName("is_enabled");
            entity.Property(image => image.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(image => image.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(image => image.Key).IsUnique();
            entity.HasIndex(image => image.Image).IsUnique();
            entity.HasIndex(image => new { image.IsEnabled, image.IsDefault });
            entity.HasIndex(image => image.IsDefault).IsUnique().HasFilter("is_enabled = true AND is_default = true");

            entity.HasData(new StorefrontDeploymentImage
            {
                Id = Guid.Parse("0aa383ff-dc89-4a30-bc13-6c4cae7b72b6"),
                Key = "storefront-v2",
                Image = "blazorshop-storefront-v2:latest",
                Version = "latest",
                IsDefault = true,
                IsEnabled = true,
                CreatedAt = new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero),
            });
        }
    }
}
