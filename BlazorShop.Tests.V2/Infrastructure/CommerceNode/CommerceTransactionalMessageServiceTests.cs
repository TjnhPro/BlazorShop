namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.Options;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    using Moq;

    using Xunit;

    public sealed class CommerceTransactionalMessageServiceTests
    {
        [Fact]
        public async Task QueueOrderPlacedAsync_QueuesOrderPlacedTemplateWithOrderTokens()
        {
            var storeId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStoreAndOrder(context, storeId, orderId);
            QueueTransactionalMessageRequest? captured = null;
            var queue = CreateQueue(request => captured = request);
            var service = CreateService(context, queue.Object);

            var result = await service.QueueOrderPlacedAsync(storeId, orderId);

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(storeId, captured.StoreId);
            Assert.Equal(TransactionalMessageTemplateSystemNames.OrderPlaced, captured.TemplateSystemName);
            Assert.Equal("customer@example.test", captured.ToEmail);
            Assert.Equal("Customer One", captured.ToName);
            Assert.Equal("en-US", captured.LanguageCode);
            Assert.Equal($"order.placed:{orderId:N}", captured.IdempotencyKey);
            Assert.Equal("ORD-100", captured.CorrelationId);
            Assert.Equal("order", captured.RelatedEntityType);
            Assert.Equal(orderId.ToString("D"), captured.RelatedEntityId);
            Assert.Equal("Demo Store", captured.Tokens["Store.Name"]);
            Assert.Equal("https://shop.example/account/orders/ORD-100", captured.Tokens["Order.DetailUrl"]);
            Assert.Equal("https://shop.example/account/orders/ORD-100/receipt", captured.Tokens["Order.ReceiptUrl"]);
            Assert.Equal("paid", captured.Tokens["Order.PaymentStatus"]);
            Assert.Equal("not_yet_shipped", captured.Tokens["Order.ShippingStatus"]);
        }

        [Fact]
        public async Task QueuePaymentStatusChangedAsync_UsesStatusScopedIdempotency()
        {
            var storeId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStoreAndOrder(context, storeId, orderId);
            QueueTransactionalMessageRequest? captured = null;
            var queue = CreateQueue(request => captured = request);
            var service = CreateService(context, queue.Object);

            var result = await service.QueuePaymentStatusChangedAsync(storeId, orderId);

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(TransactionalMessageTemplateSystemNames.OrderPaymentStatusChanged, captured.TemplateSystemName);
            Assert.Equal($"order.payment_status_changed:{orderId:N}:paid", captured.IdempotencyKey);
        }

        [Fact]
        public async Task QueueFulfillmentStatusChangedAsync_UsesShippingStatusScopedIdempotency()
        {
            var storeId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStoreAndOrder(context, storeId, orderId);
            QueueTransactionalMessageRequest? captured = null;
            var queue = CreateQueue(request => captured = request);
            var service = CreateService(context, queue.Object);

            var result = await service.QueueFulfillmentStatusChangedAsync(storeId, orderId);

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(TransactionalMessageTemplateSystemNames.OrderFulfillmentStatusChanged, captured.TemplateSystemName);
            Assert.Equal($"order.fulfillment_status_changed:{orderId:N}:not_yet_shipped", captured.IdempotencyKey);
        }

        private static Mock<IMessageQueueService> CreateQueue(Action<QueueTransactionalMessageRequest> capture)
        {
            var queue = new Mock<IMessageQueueService>();
            queue
                .Setup(service => service.QueueAsync(
                    It.IsAny<QueueTransactionalMessageRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<QueueTransactionalMessageRequest, CancellationToken>((request, _) => capture(request))
                .ReturnsAsync(new QueuedMessageResult(true, Guid.NewGuid()));
            return queue;
        }

        private static CommerceTransactionalMessageService CreateService(
            CommerceNodeDbContext context,
            IMessageQueueService queue)
        {
            return new CommerceTransactionalMessageService(
                context,
                queue,
                Options.Create(new ClientAppOptions { BaseUrl = "https://fallback.example" }));
        }

        private static void SeedStoreAndOrder(CommerceNodeDbContext context, Guid storeId, Guid orderId)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Demo Store",
                BaseUrl = "https://shop.example",
                DefaultCulture = "en-US",
                SupportEmail = "support@example.test",
            });

            context.Orders.Add(new Order
            {
                Id = orderId,
                StoreId = storeId,
                Reference = "ORD-100",
                UserId = "customer-user",
                CustomerName = "Customer One",
                CustomerEmail = "customer@example.test",
                CurrencyCode = "USD",
                TotalAmount = 42m,
                OrderStatus = OrderStatuses.Processing,
                PaymentStatus = PaymentStatuses.Paid,
                ShippingStatus = ShippingStatuses.NotYetShipped,
                StoreNameSnapshot = "Demo Store",
                StoreBaseUrlSnapshot = "https://shop.example",
                CreatedOn = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc),
            });

            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-transactional-message-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
