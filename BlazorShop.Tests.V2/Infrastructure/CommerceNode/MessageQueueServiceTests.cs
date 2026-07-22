namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class MessageQueueServiceTests
    {
        [Fact]
        public async Task QueueAsync_CreatesQueuedMessageAndDeliveryTask()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.MessageTemplates.Add(CreateTemplate(TransactionalMessageTemplateSystemNames.OrderPlaced));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.QueueAsync(new QueueTransactionalMessageRequest(
                storeId,
                TransactionalMessageTemplateSystemNames.OrderPlaced,
                "customer@example.test",
                "Customer One",
                null,
                new Dictionary<string, string?>
                {
                    ["Order.Reference"] = "ORD-1",
                    ["Contact.Message"] = "<script>alert(1)</script>",
                },
                IdempotencyKey: "order:ORD-1:placed",
                CorrelationId: "corr-1",
                RelatedEntityType: "order",
                RelatedEntityId: "ORD-1"));

            Assert.True(result.Success);
            var message = await context.QueuedMessages.SingleAsync();
            Assert.Equal(result.QueuedMessagePublicId, message.PublicId);
            Assert.Equal(QueuedMessageStatuses.Pending, message.Status);
            Assert.Equal("Order ORD-1", message.Subject);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", message.BodyHtml, StringComparison.Ordinal);
            Assert.Equal("sender@example.test", message.FromEmail);
            Assert.Equal("reply@example.test", message.ReplyToEmail);
            Assert.Equal("order:ORD-1:placed", message.IdempotencyKey);

            var task = await context.CommerceTasks.SingleAsync();
            Assert.Equal(TransactionalMessageTaskTypes.Deliver, task.TaskType);
            Assert.Equal($"message.deliver:{message.PublicId:D}", task.IdempotencyKey);
            Assert.Contains(message.PublicId.ToString(), task.PayloadJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task QueueAsync_WithSameIdempotencyKey_ReturnsExistingQueuedMessage()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.MessageTemplates.Add(CreateTemplate(TransactionalMessageTemplateSystemNames.OrderPlaced));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var request = new QueueTransactionalMessageRequest(
                storeId,
                TransactionalMessageTemplateSystemNames.OrderPlaced,
                "customer@example.test",
                "Customer One",
                null,
                new Dictionary<string, string?> { ["Order.Reference"] = "ORD-1" },
                IdempotencyKey: "order:ORD-1:placed");

            var first = await service.QueueAsync(request);
            var second = await service.QueueAsync(request);

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.QueuedMessagePublicId, second.QueuedMessagePublicId);
            Assert.Equal(1, await context.QueuedMessages.CountAsync());
            Assert.Equal(1, await context.CommerceTasks.CountAsync());
        }

        private static MessageQueueService CreateService(CommerceNodeDbContext context)
        {
            return new MessageQueueService(
                context,
                new MessageTemplateResolver(context),
                new MessageTokenRenderer(),
                new CommerceTaskService(context),
                new StubStoreEmailTransportResolver());
        }

        private static MessageTemplate CreateTemplate(string systemName)
        {
            return new MessageTemplate
            {
                SystemName = systemName,
                SubjectTemplate = "Order {{Order.Reference}}",
                BodyHtmlTemplate = "<p>{{Contact.Message}}</p>",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"message-queue-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StubStoreEmailTransportResolver : IStoreEmailTransportResolver
        {
            public Task<StoreEmailSenderProfile> ResolveSenderProfileAsync(
                Guid storeId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new StoreEmailSenderProfile(
                    "sender@example.test",
                    "Store Sender",
                    "reply@example.test",
                    FromStoreSettings: true));
            }

            public Task<StoreEmailTransportResolutionResult> ResolveTransportAsync(
                Guid storeId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
