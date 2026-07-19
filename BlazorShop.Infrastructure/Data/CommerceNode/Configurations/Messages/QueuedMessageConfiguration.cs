namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Messages
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class QueuedMessageConfiguration : IEntityTypeConfiguration<QueuedMessage>
    {
        public void Configure(EntityTypeBuilder<QueuedMessage> entity)
        {
            entity.ToTable("queued_messages", table =>
            {
                table.HasCheckConstraint(
                    "ck_queued_messages_status",
                    $"status in ({CommerceNodeSql.In(QueuedMessageStatuses.All)})");
                table.HasCheckConstraint("ck_queued_messages_attempt_count", "attempt_count >= 0");
                table.HasCheckConstraint("ck_queued_messages_max_attempts", "max_attempts >= 1");
                table.HasCheckConstraint("ck_queued_messages_priority", "priority >= 0");
            });
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id");
            entity.Property(message => message.PublicId).HasColumnName("public_id");
            entity.Property(message => message.StoreId).HasColumnName("store_id");
            entity.Property(message => message.TemplateSystemName).HasColumnName("template_system_name").HasMaxLength(128).IsRequired();
            entity.Property(message => message.TemplateId).HasColumnName("template_id");
            entity.Property(message => message.LanguageCode).HasColumnName("language_code").HasMaxLength(16);
            entity.Property(message => message.ToEmail).HasColumnName("to_email").HasMaxLength(256).IsRequired();
            entity.Property(message => message.ToName).HasColumnName("to_name").HasMaxLength(256);
            entity.Property(message => message.FromEmail).HasColumnName("from_email").HasMaxLength(256).IsRequired();
            entity.Property(message => message.FromName).HasColumnName("from_name").HasMaxLength(256);
            entity.Property(message => message.ReplyToEmail).HasColumnName("reply_to_email").HasMaxLength(256);
            entity.Property(message => message.Subject).HasColumnName("subject").HasMaxLength(512).IsRequired();
            entity.Property(message => message.BodyHtml).HasColumnName("body_html").HasColumnType("text").IsRequired();
            entity.Property(message => message.Status).HasColumnName("status").HasMaxLength(32).HasDefaultValue(QueuedMessageStatuses.Pending).IsRequired();
            entity.Property(message => message.Priority).HasColumnName("priority").HasDefaultValue(0);
            entity.Property(message => message.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
            entity.Property(message => message.MaxAttempts).HasColumnName("max_attempts").HasDefaultValue(3);
            entity.Property(message => message.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(message => message.LastAttemptAtUtc).HasColumnName("last_attempt_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(message => message.SentAtUtc).HasColumnName("sent_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(message => message.FailedAtUtc).HasColumnName("failed_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(message => message.ErrorCode).HasColumnName("error_code").HasMaxLength(128);
            entity.Property(message => message.ErrorMessage).HasColumnName("error_message").HasMaxLength(1024);
            entity.Property(message => message.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            entity.Property(message => message.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(256);
            entity.Property(message => message.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(128);
            entity.Property(message => message.RelatedEntityId).HasColumnName("related_entity_id").HasMaxLength(128);
            entity.Property(message => message.AttachmentMetadataJson).HasColumnName("attachment_metadata_json").HasColumnType("jsonb");
            entity.Property(message => message.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(message => message.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(message => message.Store)
                .WithMany()
                .HasForeignKey(message => message.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(message => message.Template)
                .WithMany(template => template.QueuedMessages)
                .HasForeignKey(message => message.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(message => message.PublicId).IsUnique();
            entity.HasIndex(message => message.IdempotencyKey)
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(message => new { message.StoreId, message.Status, message.NextAttemptAtUtc });
            entity.HasIndex(message => new { message.StoreId, message.TemplateSystemName, message.CreatedAtUtc });
            entity.HasIndex(message => new { message.StoreId, message.RelatedEntityType, message.RelatedEntityId });
            entity.HasIndex(message => message.CorrelationId);
        }
    }
}
