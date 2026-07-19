namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class TransactionalMessageAdminService : ITransactionalMessageAdminService
    {
        private const int MaxTake = 100;

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IMessageTokenRenderer tokenRenderer;
        private readonly IMessageDeliveryService deliveryService;
        private readonly IAdminAuditService auditService;

        public TransactionalMessageAdminService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IMessageTokenRenderer tokenRenderer,
            IMessageDeliveryService deliveryService,
            IAdminAuditService auditService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.tokenRenderer = tokenRenderer;
            this.deliveryService = deliveryService;
            this.auditService = auditService;
        }

        public async Task<ServiceResponse<IReadOnlyList<MessageTemplateAdminSummary>>> ListTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<IReadOnlyList<MessageTemplateAdminSummary>>(store.Message, ServiceResponseType.ValidationError);
            }

            var templates = await this.context.MessageTemplates
                .AsNoTracking()
                .Where(template => template.StoreId == null || template.StoreId == store.Payload)
                .OrderBy(template => template.SystemName)
                .ThenBy(template => template.LanguageCode)
                .ThenByDescending(template => template.StoreId == store.Payload)
                .ToArrayAsync(cancellationToken);

            var effective = templates
                .GroupBy(template => $"{template.SystemName}|{template.LanguageCode}", StringComparer.Ordinal)
                .Select(group => group.FirstOrDefault(template => template.StoreId == store.Payload) ?? group.First())
                .Select(template => new MessageTemplateAdminSummary(
                    template.PublicId,
                    template.SystemName,
                    template.LanguageCode,
                    template.StoreId == store.Payload,
                    template.IsActive,
                    template.SubjectTemplate,
                    template.Description,
                    template.UpdatedAtUtc))
                .ToArray();

            return Success<IReadOnlyList<MessageTemplateAdminSummary>>(effective, "Message templates loaded.");
        }

        public async Task<ServiceResponse<MessageTemplateAdminDetail>> GetTemplateAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<MessageTemplateAdminDetail>(store.Message, ServiceResponseType.ValidationError);
            }

            var template = await this.FindTemplateForStoreAsync(publicId, store.Payload, tracking: false, cancellationToken);
            return template is null
                ? Failure<MessageTemplateAdminDetail>("Message template was not found.", ServiceResponseType.NotFound)
                : Success(MapTemplate(template, store.Payload), "Message template loaded.");
        }

        public async Task<ServiceResponse<MessageTemplateAdminDetail>> UpdateTemplateAsync(
            Guid publicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validation = ValidateTemplateUpdate(request);
            if (validation is not null)
            {
                return Failure<MessageTemplateAdminDetail>(validation, ServiceResponseType.ValidationError);
            }

            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<MessageTemplateAdminDetail>(store.Message, ServiceResponseType.ValidationError);
            }

            var target = await this.FindTemplateForStoreAsync(publicId, store.Payload, tracking: true, cancellationToken);
            if (target is null)
            {
                return Failure<MessageTemplateAdminDetail>("Message template was not found.", ServiceResponseType.NotFound);
            }

            var languageCode = NormalizeOptional(request.LanguageCode) ?? target.LanguageCode;
            var template = target.StoreId == store.Payload
                ? target
                : await this.context.MessageTemplates.FirstOrDefaultAsync(
                    candidate => candidate.StoreId == store.Payload
                                 && candidate.SystemName == target.SystemName
                                 && candidate.LanguageCode == languageCode,
                    cancellationToken);

            var now = DateTimeOffset.UtcNow;
            if (template is null)
            {
                template = new MessageTemplate
                {
                    Id = Guid.NewGuid(),
                    PublicId = Guid.NewGuid(),
                    StoreId = store.Payload,
                    SystemName = target.SystemName,
                    LanguageCode = languageCode,
                    CreatedAtUtc = now,
                };
                this.context.MessageTemplates.Add(template);
            }
            else if (!string.Equals(template.LanguageCode, languageCode, StringComparison.Ordinal)
                     && await this.context.MessageTemplates.AnyAsync(
                         candidate => candidate.Id != template.Id
                                      && candidate.StoreId == store.Payload
                                      && candidate.SystemName == template.SystemName
                                      && candidate.LanguageCode == languageCode,
                         cancellationToken))
            {
                return Failure<MessageTemplateAdminDetail>(
                    "A store override already exists for this template language.",
                    ServiceResponseType.Conflict);
            }

            template.SubjectTemplate = request.SubjectTemplate.Trim();
            template.BodyHtmlTemplate = request.BodyHtmlTemplate.Trim();
            template.Description = NormalizeOptional(request.Description);
            template.LanguageCode = languageCode;
            template.IsActive = request.IsActive;
            template.UpdatedAtUtc = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("TransactionalMessage.TemplateUpdated", nameof(MessageTemplate), template.PublicId, new
            {
                template.StoreId,
                template.SystemName,
                template.LanguageCode,
                template.IsActive,
            });

            return Success(MapTemplate(template, store.Payload), "Message template updated.");
        }

        public async Task<ServiceResponse<MessageTemplateAdminDetail>> ResetTemplateAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<MessageTemplateAdminDetail>(store.Message, ServiceResponseType.ValidationError);
            }

            var target = await this.FindTemplateForStoreAsync(publicId, store.Payload, tracking: true, cancellationToken);
            if (target is null)
            {
                return Failure<MessageTemplateAdminDetail>("Message template was not found.", ServiceResponseType.NotFound);
            }

            var global = await this.context.MessageTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(template => template.StoreId == null
                                                 && template.SystemName == target.SystemName
                                                 && template.LanguageCode == target.LanguageCode,
                    cancellationToken);
            if (global is null)
            {
                return Failure<MessageTemplateAdminDetail>("Default message template was not found.", ServiceResponseType.NotFound);
            }

            var storeOverride = target.StoreId == store.Payload
                ? target
                : await this.context.MessageTemplates.FirstOrDefaultAsync(
                    template => template.StoreId == store.Payload
                                && template.SystemName == target.SystemName
                                && template.LanguageCode == target.LanguageCode,
                    cancellationToken);
            if (storeOverride is not null)
            {
                this.context.MessageTemplates.Remove(storeOverride);
                await this.context.SaveChangesAsync(cancellationToken);
                await this.LogAsync("TransactionalMessage.TemplateReset", nameof(MessageTemplate), storeOverride.PublicId, new
                {
                    StoreId = store.Payload,
                    storeOverride.SystemName,
                    storeOverride.LanguageCode,
                });
            }

            return Success(MapTemplate(global, store.Payload), "Message template reset to default.");
        }

        public async Task<ServiceResponse<MessageTemplatePreviewResponse>> PreviewTemplateAsync(
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.SystemName))
            {
                return Failure<MessageTemplatePreviewResponse>("Template system name is required.", ServiceResponseType.ValidationError);
            }

            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<MessageTemplatePreviewResponse>(store.Message, ServiceResponseType.ValidationError);
            }

            var resolved = await this.ResolveTemplateAsync(request.SystemName.Trim(), NormalizeOptional(request.LanguageCode), store.Payload, cancellationToken);
            if (resolved is null && (string.IsNullOrWhiteSpace(request.SubjectTemplate) || string.IsNullOrWhiteSpace(request.BodyHtmlTemplate)))
            {
                return Failure<MessageTemplatePreviewResponse>("Message template was not found.", ServiceResponseType.NotFound);
            }

            var subjectTemplate = NormalizeOptional(request.SubjectTemplate) ?? resolved!.SubjectTemplate;
            var bodyTemplate = NormalizeOptional(request.BodyHtmlTemplate) ?? resolved!.BodyHtmlTemplate;
            var subject = this.tokenRenderer.Render(new MessageTokenRenderRequest(subjectTemplate, request.Tokens));
            var body = this.tokenRenderer.Render(new MessageTokenRenderRequest(bodyTemplate, request.Tokens));
            var warnings = subject.Warnings.Concat(body.Warnings).ToArray();

            return Success(new MessageTemplatePreviewResponse(subject.Rendered, body.Rendered, warnings), "Message template preview rendered.");
        }

        public async Task<ServiceResponse<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            string? status,
            string? templateSystemName,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<QueuedMessageAdminListResponse>(store.Message, ServiceResponseType.ValidationError);
            }

            var normalizedSkip = Math.Max(0, skip);
            var normalizedTake = Math.Clamp(take <= 0 ? 25 : take, 1, MaxTake);
            var query = this.context.QueuedMessages
                .AsNoTracking()
                .Where(message => message.StoreId == store.Payload);

            var normalizedStatus = NormalizeOptional(status);
            if (normalizedStatus is not null)
            {
                query = query.Where(message => message.Status == normalizedStatus);
            }

            var normalizedSystemName = NormalizeOptional(templateSystemName);
            if (normalizedSystemName is not null)
            {
                query = query.Where(message => message.TemplateSystemName == normalizedSystemName);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(message => message.CreatedAtUtc)
                .Skip(normalizedSkip)
                .Take(normalizedTake)
                .Select(message => new QueuedMessageAdminSummary(
                    message.PublicId,
                    message.TemplateSystemName,
                    message.Status,
                    message.ToEmail,
                    message.ToName,
                    message.Subject,
                    message.AttemptCount,
                    message.MaxAttempts,
                    message.NextAttemptAtUtc,
                    message.SentAtUtc,
                    message.FailedAtUtc,
                    message.CreatedAtUtc))
                .ToArrayAsync(cancellationToken);

            return Success(new QueuedMessageAdminListResponse(items, totalCount, normalizedSkip, normalizedTake), "Queued messages loaded.");
        }

        public async Task<ServiceResponse<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<QueuedMessageAdminDetail>(store.Message, ServiceResponseType.ValidationError);
            }

            var message = await this.FindQueuedMessageAsync(publicId, store.Payload, tracking: false, cancellationToken);
            return message is null
                ? Failure<QueuedMessageAdminDetail>("Queued message was not found.", ServiceResponseType.NotFound)
                : Success(MapQueuedMessage(message), "Queued message loaded.");
        }

        public async Task<ServiceResponse<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<QueuedMessageAdminDetail>(store.Message, ServiceResponseType.ValidationError);
            }

            var message = await this.FindQueuedMessageAsync(publicId, store.Payload, tracking: false, cancellationToken);
            if (message is null)
            {
                return Failure<QueuedMessageAdminDetail>("Queued message was not found.", ServiceResponseType.NotFound);
            }

            var retry = await this.deliveryService.RetryAsync(publicId, cancellationToken);
            if (!retry.Success)
            {
                return Failure<QueuedMessageAdminDetail>(retry.Message, ServiceResponseType.Conflict);
            }

            await this.LogAsync("TransactionalMessage.QueuedMessageRetried", nameof(QueuedMessage), publicId, new
            {
                StoreId = store.Payload,
                message.TemplateSystemName,
                message.Status,
            });

            return await this.GetQueuedMessageAsync(publicId, cancellationToken);
        }

        public async Task<ServiceResponse<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.ResolveStoreIdAsync(cancellationToken);
            if (!store.Success)
            {
                return Failure<QueuedMessageAdminDetail>(store.Message, ServiceResponseType.ValidationError);
            }

            var message = await this.FindQueuedMessageAsync(publicId, store.Payload, tracking: false, cancellationToken);
            if (message is null)
            {
                return Failure<QueuedMessageAdminDetail>("Queued message was not found.", ServiceResponseType.NotFound);
            }

            var cancel = await this.deliveryService.CancelAsync(publicId, cancellationToken);
            if (!cancel.Success)
            {
                return Failure<QueuedMessageAdminDetail>(cancel.Message, ServiceResponseType.Conflict);
            }

            await this.LogAsync("TransactionalMessage.QueuedMessageCancelled", nameof(QueuedMessage), publicId, new
            {
                StoreId = store.Payload,
                message.TemplateSystemName,
                message.Status,
            });

            return await this.GetQueuedMessageAsync(publicId, cancellationToken);
        }

        private async Task<MessageTemplate?> FindTemplateForStoreAsync(
            Guid publicId,
            Guid storeId,
            bool tracking,
            CancellationToken cancellationToken)
        {
            var query = tracking ? this.context.MessageTemplates : this.context.MessageTemplates.AsNoTracking();
            var target = await query.FirstOrDefaultAsync(
                template => template.PublicId == publicId && (template.StoreId == null || template.StoreId == storeId),
                cancellationToken);
            if (target is null || target.StoreId == storeId)
            {
                return target;
            }

            return await query.FirstOrDefaultAsync(
                       template => template.StoreId == storeId
                                   && template.SystemName == target.SystemName
                                   && template.LanguageCode == target.LanguageCode,
                       cancellationToken)
                   ?? target;
        }

        private async Task<MessageTemplate?> ResolveTemplateAsync(
            string systemName,
            string? languageCode,
            Guid storeId,
            CancellationToken cancellationToken)
        {
            var candidates = await this.context.MessageTemplates
                .AsNoTracking()
                .Where(template => template.SystemName == systemName
                                   && (template.StoreId == null || template.StoreId == storeId)
                                   && (template.LanguageCode == languageCode || template.LanguageCode == null))
                .OrderByDescending(template => template.StoreId == storeId)
                .ThenByDescending(template => template.LanguageCode == languageCode)
                .ToArrayAsync(cancellationToken);

            return candidates.FirstOrDefault(template => template.IsActive);
        }

        private async Task<QueuedMessage?> FindQueuedMessageAsync(
            Guid publicId,
            Guid storeId,
            bool tracking,
            CancellationToken cancellationToken)
        {
            var query = tracking ? this.context.QueuedMessages : this.context.QueuedMessages.AsNoTracking();
            return await query.FirstOrDefaultAsync(
                message => message.PublicId == publicId && message.StoreId == storeId,
                cancellationToken);
        }

        private async Task<ApplicationResult<Guid>> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            return await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
        }

        private async Task LogAsync(string action, string entityType, Guid publicId, object metadata)
        {
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = entityType,
                EntityId = publicId.ToString(),
                Summary = action,
                MetadataJson = JsonSerializer.Serialize(metadata),
            });
        }

        private static MessageTemplateAdminDetail MapTemplate(MessageTemplate template, Guid storeId)
        {
            return new MessageTemplateAdminDetail(
                template.PublicId,
                template.SystemName,
                template.LanguageCode,
                template.StoreId == storeId,
                template.IsActive,
                template.SubjectTemplate,
                template.BodyHtmlTemplate,
                template.Description,
                template.CreatedAtUtc,
                template.UpdatedAtUtc);
        }

        private static QueuedMessageAdminDetail MapQueuedMessage(QueuedMessage message)
        {
            return new QueuedMessageAdminDetail(
                message.PublicId,
                message.TemplateSystemName,
                message.Status,
                message.ToEmail,
                message.ToName,
                message.FromEmail,
                message.FromName,
                message.Subject,
                message.AttemptCount,
                message.MaxAttempts,
                message.NextAttemptAtUtc,
                message.LastAttemptAtUtc,
                message.SentAtUtc,
                message.FailedAtUtc,
                message.ErrorCode,
                message.ErrorMessage,
                message.CorrelationId,
                message.RelatedEntityType,
                message.RelatedEntityId,
                message.CreatedAtUtc,
                message.UpdatedAtUtc);
        }

        private static string? ValidateTemplateUpdate(UpdateMessageTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SubjectTemplate) || request.SubjectTemplate.Trim().Length > 512)
            {
                return "Subject template is required and must be 512 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.BodyHtmlTemplate) || request.BodyHtmlTemplate.Trim().Length > 20000)
            {
                return "Body HTML template is required and must be 20,000 characters or fewer.";
            }

            if (request.Description?.Length > 1024)
            {
                return "Description must be 1,024 characters or fewer.";
            }

            if (NormalizeOptional(request.LanguageCode)?.Length > 16)
            {
                return "Language code must be 16 characters or fewer.";
            }

            return null;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<TPayload> Success<TPayload>(TPayload payload, string message)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failure<TPayload>(
            string? message,
            ServiceResponseType responseType)
        {
            return new ServiceResponse<TPayload>(false, message ?? "Transactional message request could not be completed.")
            {
                ResponseType = responseType,
            };
        }
    }
}
