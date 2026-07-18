namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Moq;

    using Xunit;

    public sealed class StoreEmailTestSendServiceTests
    {
        [Fact]
        public async Task SendAsync_UsesResolvedStoreTransport()
        {
            var storeId = Guid.NewGuid();
            var transport = new StoreEmailTransportSettings(
                storeId,
                StoreEmailDeliveryModes.Smtp,
                "sender@example.test",
                "Sender",
                null,
                "smtp.example.test",
                587,
                true,
                "smtp-user",
                "smtp-password");
            var resolver = new Mock<IStoreEmailTransportResolver>();
            resolver
                .Setup(service => service.ResolveTransportAsync(storeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StoreEmailTransportResolutionResult(true, transport));
            var sender = new Mock<IStoreEmailTransportSender>();
            var service = new StoreEmailTestSendService(resolver.Object, sender.Object);

            var result = await service.SendAsync(
                storeId,
                new SendStoreEmailTestRequest
                {
                    ToEmail = "qa@example.test",
                    Subject = "SMTP check",
                });

            Assert.True(result.Success, result.Message);
            sender.Verify(
                email => email.SendAsync(
                    transport,
                    "qa@example.test",
                    "SMTP check",
                    It.Is<string>(body => body.Contains("BlazorShop store SMTP test", StringComparison.Ordinal)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendAsync_WhenTransportMissing_ReturnsControlledFailureWithoutSending()
        {
            var resolver = new Mock<IStoreEmailTransportResolver>();
            resolver
                .Setup(service => service.ResolveTransportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StoreEmailTransportResolutionResult(
                    false,
                    ErrorCode: "message_delivery.smtp_not_configured",
                    Message: "Store SMTP transport is not configured."));
            var sender = new Mock<IStoreEmailTransportSender>();
            var service = new StoreEmailTestSendService(resolver.Object, sender.Object);

            var result = await service.SendAsync(
                Guid.NewGuid(),
                new SendStoreEmailTestRequest { ToEmail = "qa@example.test" });

            Assert.False(result.Success);
            Assert.Equal("Store SMTP transport is not configured.", result.Message);
            sender.Verify(
                email => email.SendAsync(
                    It.IsAny<StoreEmailTransportSettings>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
