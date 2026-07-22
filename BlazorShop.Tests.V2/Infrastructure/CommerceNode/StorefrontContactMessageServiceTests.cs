namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Moq;

    using Xunit;

    public sealed class StorefrontContactMessageServiceTests
    {
        [Fact]
        public async Task SendAsync_QueuesContactFormToStoreSupportEmail()
        {
            var storeId = Guid.NewGuid();
            var queueService = new Mock<IMessageQueueService>();
            QueueTransactionalMessageRequest? captured = null;
            queueService
                .Setup(queue => queue.QueueAsync(
                    It.IsAny<QueueTransactionalMessageRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<QueueTransactionalMessageRequest, CancellationToken>((request, _) => captured = request)
                .ReturnsAsync(new QueuedMessageResult(true, Guid.NewGuid()));
            var service = new StorefrontContactMessageService(
                new FixedStoreContext(storeId),
                queueService.Object);

            var result = await service.SendAsync(new StorefrontContactMessageRequest(
                " Jane Customer ",
                " jane@example.test ",
                " Need help ",
                " <script>hello</script> "));

            Assert.True(result.Success, result.Message);
            Assert.True(result.Payload!.Accepted);
            Assert.NotNull(captured);
            Assert.Equal(storeId, captured.StoreId);
            Assert.Equal(TransactionalMessageTemplateSystemNames.StorefrontContactForm, captured.TemplateSystemName);
            Assert.Equal("support@example.test", captured.ToEmail);
            Assert.Equal("Demo Store", captured.ToName);
            Assert.Equal("en-US", captured.LanguageCode);
            Assert.Equal("storefront.contact", captured.RelatedEntityType);
            Assert.Equal("Jane Customer", captured.Tokens["Contact.Name"]);
            Assert.Equal("jane@example.test", captured.Tokens["Contact.Email"]);
            Assert.Equal("Need help", captured.Tokens["Contact.Subject"]);
            Assert.Equal("<script>hello</script>", captured.Tokens["Contact.Message"]);
        }

        [Fact]
        public async Task SendAsync_WhenRecipientMissing_ReturnsConflict()
        {
            var service = new StorefrontContactMessageService(
                new FixedStoreContext(Guid.NewGuid(), supportEmail: null, companyEmail: null),
                Mock.Of<IMessageQueueService>());

            var result = await service.SendAsync(new StorefrontContactMessageRequest(
                "Jane Customer",
                "jane@example.test",
                "Need help",
                "Message"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
        }

        [Fact]
        public async Task SendAsync_WhenInputInvalid_DoesNotQueue()
        {
            var queueService = new Mock<IMessageQueueService>();
            var service = new StorefrontContactMessageService(
                new FixedStoreContext(Guid.NewGuid()),
                queueService.Object);

            var result = await service.SendAsync(new StorefrontContactMessageRequest(
                "",
                "bad-email",
                "",
                ""));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            queueService.Verify(
                queue => queue.QueueAsync(
                    It.IsAny<QueueTransactionalMessageRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;
            private readonly string? supportEmail;
            private readonly string? companyEmail;

            public FixedStoreContext(
                Guid storeId,
                string? supportEmail = "support@example.test",
                string? companyEmail = "company@example.test")
            {
                this.storeId = storeId;
                this.supportEmail = supportEmail;
                this.companyEmail = companyEmail;
            }

            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                var store = new CommerceCurrentStore(
                    Guid.NewGuid(),
                    "default",
                    "Demo Store",
                    CommerceStoreStatuses.Active,
                    "https://shop.example",
                    "shop.example",
                    true,
                    null,
                    null,
                    "Demo Company",
                    this.companyEmail,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "USD",
                    "en-US",
                    this.supportEmail,
                    null,
                    false,
                    null,
                    null);

                return Task.FromResult(new ApplicationResult<CommerceCurrentStore>(
                    true,
                    "Store resolved.",
                    store));
            }

            public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ApplicationResult<Guid>(
                    true,
                    "Store resolved.",
                    this.storeId));
            }
        }
    }
}
