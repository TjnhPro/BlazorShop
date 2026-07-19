namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Content
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StorefrontPageConfiguration : IEntityTypeConfiguration<StorefrontPage>
    {
        public void Configure(EntityTypeBuilder<StorefrontPage> entity)
        {
            entity.ToTable("storefront_page");
            entity.HasKey(page => page.Id);
            entity.Property(page => page.Id).HasColumnName("id");
            entity.Property(page => page.PublicId).HasColumnName("public_id");
            entity.Property(page => page.StoreId).HasColumnName("store_id");
            entity.Property(page => page.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
            entity.Property(page => page.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(page => page.Intro).HasColumnName("intro").HasMaxLength(1000);
            entity.Property(page => page.BodyHtml).HasColumnName("body_html").IsRequired();
            entity.Property(page => page.IsPublished).HasColumnName("is_published").HasDefaultValue(false);
            entity.Property(page => page.IncludeInSitemap).HasColumnName("include_in_sitemap").HasDefaultValue(false);
            entity.Property(page => page.PageKey).HasColumnName("page_key").HasMaxLength(80);
            entity.Property(page => page.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
            entity.Property(page => page.IncludeInNavigation).HasColumnName("include_in_navigation").HasDefaultValue(false);
            entity.Property(page => page.NavigationLocation).HasColumnName("navigation_location").HasMaxLength(50);
            entity.Property(page => page.MetaTitle).HasColumnName("meta_title").HasMaxLength(400);
            entity.Property(page => page.MetaDescription).HasColumnName("meta_description").HasMaxLength(4000);
            entity.Property(page => page.CanonicalUrl).HasColumnName("canonical_url").HasMaxLength(2048);
            entity.Property(page => page.OgTitle).HasColumnName("og_title").HasMaxLength(400);
            entity.Property(page => page.OgDescription).HasColumnName("og_description").HasMaxLength(4000);
            entity.Property(page => page.OgImage).HasColumnName("og_image").HasMaxLength(2048);
            entity.Property(page => page.RobotsIndex).HasColumnName("robots_index").HasDefaultValue(true);
            entity.Property(page => page.RobotsFollow).HasColumnName("robots_follow").HasDefaultValue(true);
            entity.Property(page => page.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(page => page.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(page => page.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(page => page.PublicId).IsUnique();
            entity.HasIndex(page => new { page.StoreId, page.Slug }).IsUnique();
            entity.HasIndex(page => new { page.StoreId, page.PageKey })
                .IsUnique()
                .HasFilter("page_key IS NOT NULL AND archived_at IS NULL");
            entity.HasIndex(page => new { page.StoreId, page.PageKey, page.ArchivedAt });
            entity.HasIndex(page => new { page.StoreId, page.IncludeInNavigation, page.IsPublished, page.ArchivedAt, page.DisplayOrder });
            entity.HasIndex(page => new { page.StoreId, page.IsPublished, page.ArchivedAt });
            entity.HasIndex(page => new { page.StoreId, page.IncludeInSitemap, page.IsPublished, page.ArchivedAt });
            entity.HasIndex(page => new { page.StoreId, page.UpdatedAt });

            entity.HasOne(page => page.Store)
                .WithMany()
                .HasForeignKey(page => page.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
