namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.Extensions.Options;

    using Moq;

    using Xunit;

    public sealed class QueuedAccountEmailDispatcherTests
    {
        [Fact]
        public async Task SendActivationAsync_QueuesStoreScopedTemplateMessage()
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

            var dispatcher = CreateDispatcher(storeId, queueService.Object);

            var result = await dispatcher.SendActivationAsync(
                new AccountEmailDispatchRequest(
                    " customer@example.test ",
                    "Ada Lovelace",
                    "https://store.example/confirm?token=secret",
                    "user-id"));

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(storeId, captured.StoreId);
            Assert.Equal(TransactionalMessageTemplateSystemNames.AccountActivation, captured.TemplateSystemName);
            Assert.Equal("customer@example.test", captured.ToEmail);
            Assert.Equal("Ada Lovelace", captured.ToName);
            Assert.Equal("en-US", captured.LanguageCode);
            Assert.Null(captured.IdempotencyKey);
            Assert.Equal("account.activation:user-id", captured.CorrelationId);
            Assert.Equal("identity.user", captured.RelatedEntityType);
            Assert.Equal("user-id", captured.RelatedEntityId);
            Assert.Equal("Demo Store", captured.Tokens["Store.Name"]);
            Assert.Equal("https://store.example", captured.Tokens["Store.Url"]);
            Assert.Equal("support@example.test", captured.Tokens["Store.SupportEmail"]);
            Assert.Equal("customer@example.test", captured.Tokens["Customer.Email"]);
            Assert.Equal("Ada", captured.Tokens["Customer.FirstName"]);
            Assert.Equal("Lovelace", captured.Tokens["Customer.LastName"]);
            Assert.Equal("https://store.example/confirm?token=secret", captured.Tokens["Account.ActivationUrl"]);
        }

        [Fact]
        public async Task SendPasswordRecoveryAsync_QueuesPasswordRecoveryTemplate()
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

            var dispatcher = CreateDispatcher(storeId, queueService.Object);

            var result = await dispatcher.SendPasswordRecoveryAsync(
                new AccountEmailDispatchRequest(
                    "customer@example.test",
                    null,
                    "https://store.example/reset?token=secret",
                    "user-id"));

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(storeId, captured.StoreId);
            Assert.Equal(TransactionalMessageTemplateSystemNames.PasswordRecovery, captured.TemplateSystemName);
            Assert.Equal("customer@example.test", captured.Tokens["Customer.FullName"]);
            Assert.Equal("https://store.example/reset?token=secret", captured.Tokens["Account.PasswordResetUrl"]);
        }

        [Fact]
        public async Task SendActivationAsync_WhenStoreCannotResolve_ReturnsControlledFailure()
        {
            var storeContext = new Mock<ICommerceStoreContext>();
            storeContext
                .Setup(context => context.GetCurrentStoreAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationResult<CommerceCurrentStore>(
                    false,
                    "Store request context is required.",
                    Failure: ApplicationErrorKind.Validation));

            var queueService = new Mock<IMessageQueueService>();
            var dispatcher = new QueuedAccountEmailDispatcher(
                storeContext.Object,
                queueService.Object,
                Options.Create(new ClientAppOptions { BaseUrl = "https://fallback.example" }));

            var result = await dispatcher.SendActivationAsync(
                new AccountEmailDispatchRequest(
                    "customer@example.test",
                    "Customer",
                    "https://store.example/confirm?token=secret",
                    "user-id"));

            Assert.False(result.Success);
            Assert.Equal("account_email.store_unavailable", result.ErrorCode);
            queueService.Verify(
                queue => queue.QueueAsync(
                    It.IsAny<QueueTransactionalMessageRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static QueuedAccountEmailDispatcher CreateDispatcher(
            Guid storeId,
            IMessageQueueService queueService)
        {
            var store = new CommerceCurrentStore(
                Guid.NewGuid(),
                "default",
                "Demo Store",
                "active",
                "https://store.example",
                "store.example",
                true,
                null,
                null,
                "Demo Company",
                "company@example.test",
                "+100000000",
                null,
                null,
                null,
                null,
                null,
                null,
                "USD",
                "en-US",
                "support@example.test",
                "+199999999",
                false,
                null,
                null);

            var storeContext = new Mock<ICommerceStoreContext>();
            storeContext
                .Setup(context => context.GetCurrentStoreAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationResult<CommerceCurrentStore>(
                    true,
                    "Current store resolved.",
                    store));
            storeContext
                .Setup(context => context.GetCurrentStoreIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationResult<Guid>(
                    true,
                    "Current store resolved.",
                    storeId));

            return new QueuedAccountEmailDispatcher(
                storeContext.Object,
                queueService,
                Options.Create(new ClientAppOptions { BaseUrl = "https://fallback.example" }));
        }
    }
}
