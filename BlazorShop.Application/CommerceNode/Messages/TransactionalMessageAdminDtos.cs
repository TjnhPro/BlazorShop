namespace BlazorShop.Application.CommerceNode.Messages
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.DTOs;

    public sealed record MessageTemplateAdminSummary(
        Guid PublicId,
        string SystemName,
        string? LanguageCode,
        bool IsStoreOverride,
        bool IsActive,
        string SubjectTemplate,
        string? Description,
        DateTimeOffset UpdatedAtUtc);

    public sealed record MessageTemplateAdminDetail(
        Guid PublicId,
        string SystemName,
        string? LanguageCode,
        bool IsStoreOverride,
        bool IsActive,
        string SubjectTemplate,
        string BodyHtmlTemplate,
        string? Description,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed class UpdateMessageTemplateRequest
    {
        [Required]
        [MaxLength(512)]
        public string SubjectTemplate { get; set; } = string.Empty;

        [Required]
        [MaxLength(20000)]
        public string BodyHtmlTemplate { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Description { get; set; }

        [MaxLength(16)]
        public string? LanguageCode { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public sealed class PreviewMessageTemplateRequest
    {
        [Required]
        [MaxLength(128)]
        public string SystemName { get; set; } = string.Empty;

        [MaxLength(16)]
        public string? LanguageCode { get; set; }

        [MaxLength(512)]
        public string? SubjectTemplate { get; set; }

        [MaxLength(20000)]
        public string? BodyHtmlTemplate { get; set; }

        public Dictionary<string, string?> Tokens { get; set; } = new(StringComparer.Ordinal);
    }

    public sealed record MessageTemplatePreviewResponse(
        string Subject,
        string BodyHtml,
        IReadOnlyList<MessageTokenRenderWarning> Warnings);

    public sealed record QueuedMessageAdminSummary(
        Guid PublicId,
        string TemplateSystemName,
        string Status,
        string ToEmail,
        string? ToName,
        string Subject,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset? NextAttemptAtUtc,
        DateTimeOffset? SentAtUtc,
        DateTimeOffset? FailedAtUtc,
        DateTimeOffset CreatedAtUtc);

    public sealed record QueuedMessageAdminDetail(
        Guid PublicId,
        string TemplateSystemName,
        string Status,
        string ToEmail,
        string? ToName,
        string? FromEmail,
        string? FromName,
        string Subject,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset? NextAttemptAtUtc,
        DateTimeOffset? LastAttemptAtUtc,
        DateTimeOffset? SentAtUtc,
        DateTimeOffset? FailedAtUtc,
        string? ErrorCode,
        string? ErrorMessage,
        string? CorrelationId,
        string? RelatedEntityType,
        string? RelatedEntityId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record QueuedMessageAdminListResponse(
        IReadOnlyList<QueuedMessageAdminSummary> Items,
        int TotalCount,
        int Skip,
        int Take);

    public interface ITransactionalMessageAdminService
    {
        Task<ServiceResponse<IReadOnlyList<MessageTemplateAdminSummary>>> ListTemplatesAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<MessageTemplateAdminDetail>> GetTemplateAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<MessageTemplateAdminDetail>> UpdateTemplateAsync(
            Guid publicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<MessageTemplateAdminDetail>> ResetTemplateAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<MessageTemplatePreviewResponse>> PreviewTemplateAsync(
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            string? status,
            string? templateSystemName,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid publicId,
            CancellationToken cancellationToken = default);
    }
}
