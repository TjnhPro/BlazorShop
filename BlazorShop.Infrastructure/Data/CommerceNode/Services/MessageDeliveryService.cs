namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class MessageDeliveryService : IMessageDeliveryService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IStoreEmailTransportResolver transportResolver;
        private readonly IStoreEmailTransportSender transportSender;

        public MessageDeliveryService(
            CommerceNodeDbContext context,
            IStoreEmailTransportResolver transportResolver,
            IStoreEmailTransportSender transportSender)
        {
            this.context = context;
            this.transportResolver = transportResolver;
            this.transportSender = transportSender;
        }

        public async Task<MessageDeliveryResult> DeliverAsync(
            Guid queuedMessagePublicId,
            int attemptNumber,
            CancellationToken cancellationToken = default)
        {
            var message = await this.context.QueuedMessages
                .FirstOrDefaultAsync(entity => entity.PublicId == queuedMessagePublicId, cancellationToken);
            if (message is null)
            {
                return new MessageDeliveryResult(false, false, "queued_message.not_found", "Queued message was not found.");
            }

            if (message.Status == QueuedMessageStatuses.Cancelled)
            {
                return new MessageDeliveryResult(true, false, Message: "Queued message was cancelled.");
            }

            if (message.Status == QueuedMessageStatuses.Sent)
            {
                return new MessageDeliveryResult(true, false, Message: "Queued message was already sent.");
            }

            var now = DateTimeOffset.UtcNow;
            message.Status = QueuedMessageStatuses.Sending;
            message.AttemptCount = Math.Max(message.AttemptCount + 1, attemptNumber);
            message.LastAttemptAtUtc = now;
            message.UpdatedAtUtc = now;
            message.ErrorCode = null;
            message.ErrorMessage = null;
            await this.context.SaveChangesAsync(cancellationToken);

            try
            {
                var transportResult = await this.transportResolver.ResolveTransportAsync(
                    message.StoreId,
                    cancellationToken);
                if (!transportResult.Success || transportResult.Transport is null)
                {
                    return await this.MarkFailedAsync(
                        message,
                        transportResult.ErrorCode ?? "message_delivery.smtp_not_configured",
                        transportResult.Message ?? "Store SMTP transport is not configured.",
                        cancellationToken);
                }

                await this.transportSender.SendAsync(
                    transportResult.Transport,
                    message.ToEmail,
                    message.Subject,
                    message.BodyHtml,
                    cancellationToken);

                now = DateTimeOffset.UtcNow;
                message.Status = QueuedMessageStatuses.Sent;
                message.SentAtUtc = now;
                message.NextAttemptAtUtc = null;
                message.UpdatedAtUtc = now;
                await this.context.SaveChangesAsync(cancellationToken);

                return new MessageDeliveryResult(true, false, Message: "Queued message sent.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return await this.MarkFailedAsync(
                    message,
                    "message_delivery.smtp_failed",
                    ex.Message,
                    cancellationToken);
            }
        }

        public async Task<QueuedMessageResult> RetryAsync(
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            var message = await this.context.QueuedMessages
                .FirstOrDefaultAsync(entity => entity.PublicId == queuedMessagePublicId, cancellationToken);
            if (message is null)
            {
                return new QueuedMessageResult(false, ErrorCode: "queued_message.not_found", Message: "Queued message was not found.");
            }

            if (message.Status is not (QueuedMessageStatuses.Failed or QueuedMessageStatuses.WaitingRetry))
            {
                return new QueuedMessageResult(false, message.PublicId, "queued_message.retry_conflict", "Only failed or waiting retry messages can be retried.");
            }

            var now = DateTimeOffset.UtcNow;
            message.Status = QueuedMessageStatuses.Pending;
            message.NextAttemptAtUtc = now;
            message.FailedAtUtc = null;
            message.ErrorCode = null;
            message.ErrorMessage = null;
            message.UpdatedAtUtc = now;
            await this.context.SaveChangesAsync(cancellationToken);

            return new QueuedMessageResult(true, message.PublicId, Message: "Queued message is ready for retry.");
        }

        public async Task<QueuedMessageResult> CancelAsync(
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            var message = await this.context.QueuedMessages
                .FirstOrDefaultAsync(entity => entity.PublicId == queuedMessagePublicId, cancellationToken);
            if (message is null)
            {
                return new QueuedMessageResult(false, ErrorCode: "queued_message.not_found", Message: "Queued message was not found.");
            }

            if (message.Status == QueuedMessageStatuses.Sent)
            {
                return new QueuedMessageResult(false, message.PublicId, "queued_message.cancel_conflict", "Sent messages cannot be cancelled.");
            }

            message.Status = QueuedMessageStatuses.Cancelled;
            message.NextAttemptAtUtc = null;
            message.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);

            return new QueuedMessageResult(true, message.PublicId, Message: "Queued message cancelled.");
        }

        private static string? Truncate(string? value, int maxLength)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Length <= maxLength ? value : value[..maxLength];
        }

        private async Task<MessageDeliveryResult> MarkFailedAsync(
            QueuedMessage message,
            string errorCode,
            string? errorMessage,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var retryable = message.AttemptCount < message.MaxAttempts;
            message.Status = retryable ? QueuedMessageStatuses.WaitingRetry : QueuedMessageStatuses.Failed;
            message.NextAttemptAtUtc = retryable ? now.AddMinutes(Math.Min(60, message.AttemptCount * 5)) : null;
            message.FailedAtUtc = retryable ? null : now;
            message.ErrorCode = errorCode;
            message.ErrorMessage = Truncate(errorMessage, 1024);
            message.UpdatedAtUtc = now;
            await this.context.SaveChangesAsync(cancellationToken);

            return new MessageDeliveryResult(false, retryable, errorCode, "Queued message delivery failed.");
        }
    }
}
