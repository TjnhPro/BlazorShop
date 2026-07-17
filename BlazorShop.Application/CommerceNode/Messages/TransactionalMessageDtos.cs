namespace BlazorShop.Application.CommerceNode.Messages
{
    public sealed record MessageTemplateResolutionRequest(
        string SystemName,
        Guid? StoreId = null,
        string? LanguageCode = null);

    public sealed record MessageTemplateResolutionResult(
        bool Success,
        MessageTemplateDto? Template,
        string? ErrorCode = null,
        string? Message = null)
    {
        public static MessageTemplateResolutionResult Found(MessageTemplateDto template)
        {
            return new MessageTemplateResolutionResult(true, template);
        }

        public static MessageTemplateResolutionResult NotFound(string systemName)
        {
            return new MessageTemplateResolutionResult(false, null, "message_template.not_found", $"Message template '{systemName}' was not found.");
        }
    }

    public sealed record MessageTemplateDto(
        Guid PublicId,
        string SystemName,
        Guid? StoreId,
        string? LanguageCode,
        string SubjectTemplate,
        string BodyHtmlTemplate,
        bool IsActive,
        string? Description,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record QueueTransactionalMessageRequest(
        Guid StoreId,
        string TemplateSystemName,
        string ToEmail,
        string? ToName,
        string? LanguageCode,
        IReadOnlyDictionary<string, string?> Tokens,
        string? IdempotencyKey = null,
        string? CorrelationId = null,
        string? RelatedEntityType = null,
        string? RelatedEntityId = null);

    public sealed record QueuedMessageResult(
        bool Success,
        Guid? QueuedMessagePublicId = null,
        string? ErrorCode = null,
        string? Message = null);

    public sealed record MessageTokenRenderRequest(
        string Template,
        IReadOnlyDictionary<string, string?> Tokens,
        IReadOnlySet<string>? SafeHtmlTokens = null,
        IReadOnlySet<string>? RequiredTokens = null);

    public sealed record MessageTokenRenderResult(
        string Rendered,
        IReadOnlyList<MessageTokenRenderWarning> Warnings,
        IReadOnlyList<string> UnknownTokens,
        IReadOnlyList<string> MissingRequiredTokens);

    public sealed record MessageTokenRenderWarning(
        string Code,
        string TokenName,
        string Message);
}
