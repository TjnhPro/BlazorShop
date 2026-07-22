extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Tasks;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Tasks;

    using Moq;

    using Xunit;

    public sealed class OrderCreatedTaskHandlerTests
    {
        [Fact]
        public async Task ExecuteAsync_DeserializesWebJsonPayloadFromOrderPlacementOutbox()
        {
            var storeId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            Guid capturedStoreId = Guid.Empty;
            Guid capturedOrderId = Guid.Empty;
            var messageService = new Mock<ICommerceTransactionalMessageService>();
            messageService
                .Setup(service => service.QueueOrderPlacedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Guid, Guid, CancellationToken>((actualStoreId, actualOrderId, _) =>
                {
                    capturedStoreId = actualStoreId;
                    capturedOrderId = actualOrderId;
                })
                .ReturnsAsync(new QueuedMessageResult(true, Guid.NewGuid()));
            var handler = new OrderCreatedTaskHandler(messageService.Object);
            var context = new CommerceTaskHandlerContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "order.created",
                "v1",
                $$"""
                {
                  "orderId": "{{orderId:D}}",
                  "storeId": "{{storeId:D}}",
                  "reference": "ORD-100",
                  "customerEmail": "customer@example.local",
                  "totalAmount": 100.00,
                  "currencyCode": "EUR",
                  "createdAtUtc": "2026-07-18T00:00:00Z"
                }
                """,
                1,
                _ => Task.FromResult(false));

            var result = await handler.ExecuteAsync(context, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(storeId, capturedStoreId);
            Assert.Equal(orderId, capturedOrderId);
            messageService.Verify(
                service => service.QueueOrderPlacedAsync(storeId, orderId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
