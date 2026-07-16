namespace BlazorShop.Infrastructure.Data.Configurations
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasIndex(category => new { category.StoreId, category.Slug })
                .IsUnique()
                .HasFilter("\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            builder.HasIndex(category => category.StoreId);
            builder.HasIndex(category => new { category.StoreId, category.ParentCategoryId, category.DisplayOrder });
            builder.HasIndex(category => new { category.StoreId, category.IsPublished, category.ArchivedAt });

            builder.HasOne(category => category.ParentCategory)
                .WithMany(category => category.Children)
                .HasForeignKey(category => category.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(category => category.Description)
                .HasColumnType("text");

            builder.Property(category => category.Image)
                .HasColumnType("text");

            builder.Property(category => category.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(category => category.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(category => category.ArchivedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(category => category.Slug)
                .HasMaxLength(SeoConstraints.SlugMaxLength);

            builder.Property(category => category.MetaTitle)
                .HasMaxLength(SeoConstraints.MetaTitleMaxLength);

            builder.Property(category => category.MetaDescription)
                .HasMaxLength(SeoConstraints.MetaDescriptionMaxLength);

            builder.Property(category => category.CanonicalUrl)
                .HasMaxLength(SeoConstraints.UrlMaxLength);

            builder.Property(category => category.OgTitle)
                .HasMaxLength(SeoConstraints.MetaTitleMaxLength);

            builder.Property(category => category.OgDescription)
                .HasMaxLength(SeoConstraints.MetaDescriptionMaxLength);

            builder.Property(category => category.OgImage)
                .HasMaxLength(SeoConstraints.UrlMaxLength);

            builder.Property(category => category.RobotsIndex)
                .HasDefaultValue(true);

            builder.Property(category => category.RobotsFollow)
                .HasDefaultValue(true);

            builder.Property(category => category.SeoContent)
                .HasColumnType("text");

            builder.Property(category => category.IsPublished)
                .HasDefaultValue(true);
        }
    }
}
