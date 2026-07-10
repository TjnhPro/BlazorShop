namespace BlazorShop.Infrastructure.Data.Configurations
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasIndex(product => new { product.CategoryId, product.CreatedOn });
            builder.HasIndex(product => product.StoreId);
            builder.HasIndex(product => new { product.StoreId, product.Sku })
                .IsUnique()
                .HasFilter("\"StoreId\" IS NOT NULL AND \"Sku\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            builder.HasIndex(product => new { product.StoreId, product.CategoryId, product.DisplayOrder, product.CreatedOn });
            builder.HasIndex(product => new { product.StoreId, product.IsPublished, product.ArchivedAt });

            builder.HasIndex(product => new { product.StoreId, product.Slug })
                .IsUnique()
                .HasFilter("\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            builder.Property(product => product.Sku)
                .HasMaxLength(64);

            builder.Property(product => product.ShortDescription)
                .HasColumnType("text");

            builder.Property(product => product.FullDescription)
                .HasColumnType("text");

            builder.Property(product => product.ComparePrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(product => product.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(product => product.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(product => product.ArchivedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(product => product.Slug)
                .HasMaxLength(SeoConstraints.SlugMaxLength);

            builder.Property(product => product.MetaTitle)
                .HasMaxLength(SeoConstraints.MetaTitleMaxLength);

            builder.Property(product => product.MetaDescription)
                .HasMaxLength(SeoConstraints.MetaDescriptionMaxLength);

            builder.Property(product => product.CanonicalUrl)
                .HasMaxLength(SeoConstraints.UrlMaxLength);

            builder.Property(product => product.OgTitle)
                .HasMaxLength(SeoConstraints.MetaTitleMaxLength);

            builder.Property(product => product.OgDescription)
                .HasMaxLength(SeoConstraints.MetaDescriptionMaxLength);

            builder.Property(product => product.OgImage)
                .HasMaxLength(SeoConstraints.UrlMaxLength);

            builder.Property(product => product.RobotsIndex)
                .HasDefaultValue(true);

            builder.Property(product => product.RobotsFollow)
                .HasDefaultValue(true);

            builder.Property(product => product.SeoContent)
                .HasColumnType("text");

            builder.Property(product => product.IsPublished)
                .HasDefaultValue(true);
        }
    }
}
