namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class MessageQueueService : IMessageQueueService
    {
        private static readonly HashSet<string> SafeHtmlTokens = new(StringComparer.Ordinal)
        {
            "Account.ActivationUrl",
            "Account.PasswordResetUrl",
            "Order.DetailUrl",
            "Order.ReceiptUrl",
            "Shipment.TrackingUrl",
            "Store.Url",
        };

        private readonly CommerceNodeDbContext context;
        private readonly IMessageTemplateResolver templateResolver;
        private readonly IMessageTokenRenderer tokenRenderer;
        private readonly ICommerceTaskService taskService;
        private readonly EmailSettings emailSettings;

        public MessageQueueService(
            CommerceNodeDbContext context,
            IMessageTemplateResolver templateResolver,
            IMessageTokenRenderer tokenRenderer,
            ICommerceTaskService taskService,
            IOptions<EmailSettings> emailSettings)
        {
            this.context = context;
            this.templateResolver = templateResolver;
            this.tokenRenderer = tokenRenderer;
            this.taskService = taskService;
            this.emailSettings = emailSettings.Value;
        }

        public async Task<QueuedMessageResult> QueueAsync(
            QueueTransactionalMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationError = Validate(request);
            if (validationError is not null)
            {
                return new QueuedMessageResult(false, ErrorCode: "message_queue.validation_failed", Message: validationError);
            }

            var idempotencyKey = NormalizeOptional(request.IdempotencyKey);
            if (idempotencyKey is not null)
            {
                var existing = await this.context.QueuedMessages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(message => message.IdempotencyKey == idempotencyKey, cancellationToken);
                if (existing is not null)
                {
                    return new QueuedMessageResult(true, existing.PublicId, Message: "Queued message already exists.");
                }
            }

            var templateResult = await this.templateResolver.ResolveAsync(
                new MessageTemplateResolutionRequest(request.TemplateSystemName, request.StoreId, request.LanguageCode),
                cancellationToken);
            if (!templateResult.Success || templateResult.Template is null)
            {
                return new QueuedMessageResult(false, ErrorCode: templateResult.ErrorCode, Message: templateResult.Message);
            }

            var subject = this.tokenRenderer.Render(new MessageTokenRenderRequest(
                templateResult.Template.SubjectTemplate,
                request.Tokens,
                SafeHtmlTokens));
            var body = this.tokenRenderer.Render(new MessageTokenRenderRequest(
                templateResult.Template.BodyHtmlTemplate,
                request.Tokens,
                SafeHtmlTokens));
            var now = DateTimeOffset.UtcNow;
            var queuedMessage = new QueuedMessage
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                TemplateSystemName = templateResult.Template.SystemName,
                TemplateId = await ResolveTemplateIdAsync(templateResult.Template.PublicId, cancellationToken),
                LanguageCode = NormalizeOptional(request.LanguageCode),
                ToEmail = request.ToEmail.Trim(),
                ToName = NormalizeOptional(request.ToName),
                FromEmail = this.emailSettings.From.Trim(),
                FromName = NormalizeOptional(this.emailSettings.DisplayName),
                Subject = subject.Rendered,
                BodyHtml = body.Rendered,
                Status = QueuedMessageStatuses.Pending,
                Priority = 0,
                AttemptCount = 0,
                MaxAttempts = 3,
                NextAttemptAtUtc = now,
                CorrelationId = NormalizeOptional(request.CorrelationId),
                IdempotencyKey = idempotencyKey,
                RelatedEntityType = NormalizeOptional(request.RelatedEntityType),
                RelatedEntityId = NormalizeOptional(request.RelatedEntityId),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.QueuedMessages.Add(queuedMessage);
            await this.context.SaveChangesAsync(cancellationToken);

            var taskResult = await this.taskService.EnqueueAsync(
                new EnqueueCommerceTaskRequest(
                    TransactionalMessageTaskTypes.Deliver,
                    IdempotencyKey: $"message.deliver:{queuedMessage.PublicId:D}",
                    PayloadJson: JsonSerializer.Serialize(new MessageDeliverTaskPayload(queuedMessage.PublicId)),
                    LockKey: $"message:{queuedMessage.PublicId:D}",
                    MaxAttempts: queuedMessage.MaxAttempts,
                    CreatedBy: "message.queue",
                    CorrelationId: queuedMessage.CorrelationId),
                cancellationToken);
            if (taskResult.Success)
            {
                return new QueuedMessageResult(true, queuedMessage.PublicId, Message: "Queued message created.");
            }

            queuedMessage.Status = QueuedMessageStatuses.Failed;
            queuedMessage.ErrorCode = "message_task.enqueue_failed";
            queuedMessage.ErrorMessage = Truncate(taskResult.Message, 1024);
            queuedMessage.FailedAtUtc = DateTimeOffset.UtcNow;
            queuedMessage.UpdatedAtUtc = queuedMessage.FailedAtUtc.Value;
            await this.context.SaveChangesAsync(cancellationToken);

            return new QueuedMessageResult(false, queuedMessage.PublicId, queuedMessage.ErrorCode, queuedMessage.ErrorMessage);
        }

        private async Task<Guid?> ResolveTemplateIdAsync(Guid templatePublicId, CancellationToken cancellationToken)
        {
            return await this.context.MessageTemplates
                .Where(template => template.PublicId == templatePublicId)
                .Select(template => (Guid?)template.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private string? Validate(QueueTransactionalMessageRequest request)
        {
            if (request.StoreId == Guid.Empty)
            {
                return "Store is required.";
            }

            if (string.IsNullOrWhiteSpace(request.TemplateSystemName))
            {
                return "Template system name is required.";
            }

            if (string.IsNullOrWhiteSpace(request.ToEmail))
            {
                return "Recipient email is required.";
            }

            if (string.IsNullOrWhiteSpace(this.emailSettings.From))
            {
                return "Sender email is not configured.";
            }

            return null;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? Truncate(string? value, int maxLength)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
