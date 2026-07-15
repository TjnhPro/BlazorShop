namespace BlazorShop.Infrastructure.Data.Configurations
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreSeoSettingsConfiguration : IEntityTypeConfiguration<StoreSeoSettings>
    {
        public void Configure(EntityTypeBuilder<StoreSeoSettings> builder)
        {
            builder.ToTable("store_seo_settings");

            builder.HasKey(settings => settings.Id);
            builder.Property(settings => settings.Id).HasColumnName("id");
            builder.Property(settings => settings.StoreId).HasColumnName("store_id");
            builder.Property(settings => settings.SiteName).HasColumnName("site_name").HasMaxLength(SeoConstraints.SiteNameMaxLength);
            builder.Property(settings => settings.DefaultTitleSuffix).HasColumnName("default_title_suffix").HasMaxLength(SeoConstraints.TitleSuffixMaxLength);
            builder.Property(settings => settings.DefaultMetaDescription).HasColumnName("default_meta_description").HasMaxLength(SeoConstraints.MetaDescriptionMaxLength);
            builder.Property(settings => settings.DefaultOgImage).HasColumnName("default_og_image").HasMaxLength(SeoConstraints.UrlMaxLength);
            builder.Property(settings => settings.BaseCanonicalUrl).HasColumnName("base_canonical_url").HasMaxLength(SeoConstraints.UrlMaxLength);
            builder.Property(settings => settings.CompanyName).HasColumnName("company_name").HasMaxLength(SeoConstraints.CompanyNameMaxLength);
            builder.Property(settings => settings.CompanyLogoUrl).HasColumnName("company_logo_url").HasMaxLength(SeoConstraints.UrlMaxLength);
            builder.Property(settings => settings.CompanyPhone).HasColumnName("company_phone").HasMaxLength(SeoConstraints.CompanyPhoneMaxLength);
            builder.Property(settings => settings.CompanyEmail).HasColumnName("company_email").HasMaxLength(SeoConstraints.CompanyEmailMaxLength);
            builder.Property(settings => settings.CompanyAddress).HasColumnName("company_address").HasMaxLength(SeoConstraints.CompanyAddressMaxLength);
            builder.Property(settings => settings.FacebookUrl).HasColumnName("facebook_url").HasMaxLength(SeoConstraints.UrlMaxLength);
            builder.Property(settings => settings.InstagramUrl).HasColumnName("instagram_url").HasMaxLength(SeoConstraints.UrlMaxLength);
            builder.Property(settings => settings.XUrl).HasColumnName("x_url").HasMaxLength(SeoConstraints.UrlMaxLength);
            builder.Property(settings => settings.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(settings => settings.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasIndex(settings => settings.StoreId).IsUnique();
            builder.HasOne(settings => settings.Store)
                .WithMany()
                .HasForeignKey(settings => settings.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
