namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Moq;

    using Xunit;

    public sealed class MessageDeliveryServiceTests
    {
        [Fact]
        public async Task DeliverAsync_SendsEmailAndMarksMessageSent()
        {
            await using var context = CreateContext();
            var message = CreateMessage();
            context.QueuedMessages.Add(message);
            await context.SaveChangesAsync();
            var sender = new Mock<IStoreEmailTransportSender>();
            var service = new MessageDeliveryService(
                context,
                CreateResolver(success: true),
                sender.Object);

            var result = await service.DeliverAsync(message.PublicId, attemptNumber: 1);

            Assert.True(result.Success);
            sender.Verify(
                email => email.SendAsync(
                    It.IsAny<StoreEmailTransportSettings>(),
                    "customer@example.test",
                    "Subject",
                    "<p>Body</p>",
                    It.IsAny<CancellationToken>()),
                Times.Once);
            var updated = await context.QueuedMessages.SingleAsync();
            Assert.Equal(QueuedMessageStatuses.Sent, updated.Status);
            Assert.Equal(1, updated.AttemptCount);
            Assert.NotNull(updated.SentAtUtc);
        }

        [Fact]
        public async Task DeliverAsync_WhenSmtpFailsBeforeMaxAttempts_MarksWaitingRetry()
        {
            await using var context = CreateContext();
            var message = CreateMessage();
            context.QueuedMessages.Add(message);
            await context.SaveChangesAsync();
            var sender = new Mock<IStoreEmailTransportSender>();
            sender
                .Setup(email => email.SendAsync(
                    It.IsAny<StoreEmailTransportSettings>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("smtp down"));
            var service = new MessageDeliveryService(
                context,
                CreateResolver(success: true),
                sender.Object);

            var result = await service.DeliverAsync(message.PublicId, attemptNumber: 1);

            Assert.False(result.Success);
            Assert.True(result.Retryable);
            var updated = await context.QueuedMessages.SingleAsync();
            Assert.Equal(QueuedMessageStatuses.WaitingRetry, updated.Status);
            Assert.Equal("message_delivery.smtp_failed", updated.ErrorCode);
            Assert.NotNull(updated.NextAttemptAtUtc);
            Assert.Null(updated.FailedAtUtc);
        }

        [Fact]
        public async Task DeliverAsync_WhenSmtpFailsAtMaxAttempts_MarksFailed()
        {
            await using var context = CreateContext();
            var message = CreateMessage();
            message.AttemptCount = 2;
            message.MaxAttempts = 3;
            context.QueuedMessages.Add(message);
            await context.SaveChangesAsync();
            var sender = new Mock<IStoreEmailTransportSender>();
            sender
                .Setup(email => email.SendAsync(
                    It.IsAny<StoreEmailTransportSettings>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("smtp down"));
            var service = new MessageDeliveryService(
                context,
                CreateResolver(success: true),
                sender.Object);

            var result = await service.DeliverAsync(message.PublicId, attemptNumber: 3);

            Assert.False(result.Success);
            Assert.False(result.Retryable);
            var updated = await context.QueuedMessages.SingleAsync();
            Assert.Equal(QueuedMessageStatuses.Failed, updated.Status);
            Assert.Equal(3, updated.AttemptCount);
            Assert.NotNull(updated.FailedAtUtc);
            Assert.Null(updated.NextAttemptAtUtc);
        }

        [Fact]
        public async Task RetryAndCancel_UpdateQueuedMessageState()
        {
            await using var context = CreateContext();
            var message = CreateMessage();
            message.Status = QueuedMessageStatuses.Failed;
            message.ErrorCode = "message_delivery.smtp_failed";
            context.QueuedMessages.Add(message);
            await context.SaveChangesAsync();
            var service = new MessageDeliveryService(
                context,
                CreateResolver(success: true),
                Mock.Of<IStoreEmailTransportSender>());

            var retry = await service.RetryAsync(message.PublicId);
            var cancel = await service.CancelAsync(message.PublicId);

            Assert.True(retry.Success);
            Assert.True(cancel.Success);
            var updated = await context.QueuedMessages.SingleAsync();
            Assert.Equal(QueuedMessageStatuses.Cancelled, updated.Status);
            Assert.Null(updated.ErrorCode);
        }

        [Fact]
        public async Task DeliverAsync_WhenStoreSmtpMissing_MarksWaitingRetryWithoutSending()
        {
            await using var context = CreateContext();
            var message = CreateMessage();
            context.QueuedMessages.Add(message);
            await context.SaveChangesAsync();
            var sender = new Mock<IStoreEmailTransportSender>();
            var service = new MessageDeliveryService(
                context,
                CreateResolver(success: false),
                sender.Object);

            var result = await service.DeliverAsync(message.PublicId, attemptNumber: 1);

            Assert.False(result.Success);
            Assert.True(result.Retryable);
            sender.Verify(
                email => email.SendAsync(
                    It.IsAny<StoreEmailTransportSettings>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            var updated = await context.QueuedMessages.SingleAsync();
            Assert.Equal(QueuedMessageStatuses.WaitingRetry, updated.Status);
            Assert.Equal("message_delivery.smtp_not_configured", updated.ErrorCode);
        }

        private static QueuedMessage CreateMessage()
        {
            return new QueuedMessage
            {
                StoreId = Guid.NewGuid(),
                TemplateSystemName = TransactionalMessageTemplateSystemNames.OrderPlaced,
                ToEmail = "customer@example.test",
                FromEmail = "sender@example.test",
                Subject = "Subject",
                BodyHtml = "<p>Body</p>",
                Status = QueuedMessageStatuses.Pending,
                MaxAttempts = 3,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"message-delivery-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static IStoreEmailTransportResolver CreateResolver(bool success)
        {
            var resolver = new Mock<IStoreEmailTransportResolver>();
            resolver
                .Setup(item => item.ResolveTransportAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(success
                    ? new StoreEmailTransportResolutionResult(
                        true,
                        new StoreEmailTransportSettings(
                            Guid.NewGuid(),
                            StoreEmailDeliveryModes.Smtp,
                            "sender@example.test",
                            "Sender",
                            null,
                            "smtp.example.test",
                            587,
                            true,
                            "smtp-user",
                            "smtp-password"))
                    : new StoreEmailTransportResolutionResult(
                        false,
                        ErrorCode: "message_delivery.smtp_not_configured",
                        Message: "Store SMTP transport is not configured."));
            return resolver.Object;
        }
    }
}
