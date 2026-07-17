namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class MessageTemplateResolver : IMessageTemplateResolver
    {
        private readonly CommerceNodeDbContext context;

        public MessageTemplateResolver(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<MessageTemplateResolutionResult> ResolveAsync(
            MessageTemplateResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.SystemName))
            {
                return MessageTemplateResolutionResult.NotFound(string.Empty);
            }

            var systemName = request.SystemName.Trim();
            var languageCode = NormalizeLanguageCode(request.LanguageCode);
            var candidates = await this.context.MessageTemplates
                .AsNoTracking()
                .Where(template => template.SystemName == systemName && template.IsActive)
                .Where(template => template.StoreId == request.StoreId || template.StoreId == null)
                .Where(template => template.LanguageCode == languageCode || template.LanguageCode == null)
                .ToListAsync(cancellationToken);

            var resolved = candidates
                .OrderByDescending(template => template.StoreId == request.StoreId)
                .ThenByDescending(template => string.Equals(template.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
                .ThenBy(template => template.StoreId.HasValue ? 0 : 1)
                .ThenBy(template => template.LanguageCode is null ? 1 : 0)
                .FirstOrDefault();

            return resolved is null
                ? MessageTemplateResolutionResult.NotFound(systemName)
                : MessageTemplateResolutionResult.Found(Map(resolved));
        }

        private static string? NormalizeLanguageCode(string? languageCode)
        {
            return string.IsNullOrWhiteSpace(languageCode)
                ? null
                : languageCode.Trim().ToLowerInvariant();
        }

        private static MessageTemplateDto Map(MessageTemplate template)
        {
            return new MessageTemplateDto(
                template.PublicId,
                template.SystemName,
                template.StoreId,
                template.LanguageCode,
                template.SubjectTemplate,
                template.BodyHtmlTemplate,
                template.IsActive,
                template.Description,
                template.CreatedAtUtc,
                template.UpdatedAtUtc);
        }
    }
}
