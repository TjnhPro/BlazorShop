namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Messages
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreEmailSettingsConfiguration : IEntityTypeConfiguration<StoreEmailSettings>
    {
        public void Configure(EntityTypeBuilder<StoreEmailSettings> entity)
        {
            entity.ToTable("store_email_settings", table =>
            {
                table.HasCheckConstraint(
                    "ck_store_email_settings_delivery_mode",
                    $"delivery_mode in ({CommerceNodeSql.In(StoreEmailDeliveryModes.All)})");
                table.HasCheckConstraint(
                    "ck_store_email_settings_smtp_port",
                    "smtp_port >= 1 AND smtp_port <= 65535");
            });
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).HasColumnName("id");
            entity.Property(settings => settings.PublicId).HasColumnName("public_id");
            entity.Property(settings => settings.StoreId).HasColumnName("store_id");
            entity.Property(settings => settings.Enabled).HasColumnName("enabled").HasDefaultValue(false);
            entity.Property(settings => settings.SmtpHost).HasColumnName("smtp_host").HasMaxLength(253);
            entity.Property(settings => settings.SmtpPort).HasColumnName("smtp_port").HasDefaultValue(587);
            entity.Property(settings => settings.UseSsl).HasColumnName("use_ssl").HasDefaultValue(true);
            entity.Property(settings => settings.Username).HasColumnName("username").HasMaxLength(320);
            entity.Property(settings => settings.ProtectedPassword).HasColumnName("protected_password").HasColumnType("text");
            entity.Property(settings => settings.PasswordUpdatedAtUtc).HasColumnName("password_updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(settings => settings.FromEmail).HasColumnName("from_email").HasMaxLength(254);
            entity.Property(settings => settings.FromDisplayName).HasColumnName("from_display_name").HasMaxLength(160);
            entity.Property(settings => settings.ReplyToEmail).HasColumnName("reply_to_email").HasMaxLength(254);
            entity.Property(settings => settings.DeliveryMode).HasColumnName("delivery_mode").HasMaxLength(32).HasDefaultValue(StoreEmailDeliveryModes.Smtp).IsRequired();
            entity.Property(settings => settings.CaptureRedirectToEmail).HasColumnName("capture_redirect_to_email").HasMaxLength(254);
            entity.Property(settings => settings.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(settings => settings.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(settings => settings.UpdatedByUserId).HasColumnName("updated_by_user_id").HasMaxLength(128);

            entity.HasOne(settings => settings.Store)
                .WithMany()
                .HasForeignKey(settings => settings.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(settings => settings.PublicId).IsUnique();
            entity.HasIndex(settings => settings.StoreId).IsUnique();
            entity.HasIndex(settings => new { settings.StoreId, settings.Enabled, settings.DeliveryMode });
        }
    }
}
