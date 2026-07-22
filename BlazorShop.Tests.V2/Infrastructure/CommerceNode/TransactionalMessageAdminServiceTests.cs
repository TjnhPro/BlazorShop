namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Moq;

    using Xunit;

    public sealed class TransactionalMessageAdminServiceTests
    {
        [Fact]
        public async Task UpdateTemplateAsync_WhenTargetIsGlobal_CreatesStoreOverride()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var global = CreateTemplate(storeId: null, TransactionalMessageTemplateSystemNames.OrderPlaced);
            context.MessageTemplates.Add(global);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.UpdateTemplateAsync(
                global.PublicId,
                new UpdateMessageTemplateRequest
                {
                    SubjectTemplate = "Store subject {{Order.Reference}}",
                    BodyHtmlTemplate = "<p>Store body</p>",
                    Description = "Store override",
                    IsActive = true,
                });

            Assert.True(result.Success, result.Message);
            Assert.True(result.Payload!.IsStoreOverride);
            Assert.NotEqual(global.PublicId, result.Payload.PublicId);
            Assert.Equal(2, await context.MessageTemplates.CountAsync());
            var original = await context.MessageTemplates.SingleAsync(template => template.PublicId == global.PublicId);
            Assert.Null(original.StoreId);
            Assert.Equal("Default subject", original.SubjectTemplate);
        }

        [Fact]
        public async Task ResetTemplateAsync_RemovesStoreOverrideAndReturnsGlobalTemplate()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var global = CreateTemplate(storeId: null, TransactionalMessageTemplateSystemNames.OrderPlaced);
            var storeOverride = CreateTemplate(storeId, TransactionalMessageTemplateSystemNames.OrderPlaced);
            storeOverride.SubjectTemplate = "Override subject";
            context.MessageTemplates.AddRange(global, storeOverride);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.ResetTemplateAsync(storeOverride.PublicId);

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.IsStoreOverride);
            Assert.Equal(global.PublicId, result.Payload.PublicId);
            Assert.Single(await context.MessageTemplates.ToArrayAsync());
        }

        [Fact]
        public async Task QueuedMessageDetail_DoesNotExposeRenderedBodyHtml()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var message = CreateMessage(storeId);
            message.BodyHtml = "<a href=\"https://shop.example/reset?token=secret-token\">reset</a>";
            context.QueuedMessages.Add(message);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.GetQueuedMessageAsync(message.PublicId);

            Assert.True(result.Success, result.Message);
            Assert.Equal(message.Subject, result.Payload!.Subject);
            Assert.Null(typeof(QueuedMessageAdminDetail).GetProperty("BodyHtml"));
            Assert.Null(typeof(QueuedMessageAdminDetail).GetProperty("IdempotencyKey"));
        }

        [Fact]
        public async Task RetryQueuedMessageAsync_IsStoreScopedAndUpdatesFailedMessage()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var message = CreateMessage(storeId);
            message.Status = QueuedMessageStatuses.Failed;
            message.ErrorCode = "message_delivery.smtp_failed";
            context.QueuedMessages.Add(message);
            context.QueuedMessages.Add(CreateMessage(Guid.NewGuid()));
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var retry = await service.RetryQueuedMessageAsync(message.PublicId);
            var otherStore = await service.RetryQueuedMessageAsync(
                await context.QueuedMessages
                    .Where(candidate => candidate.StoreId != storeId)
                    .Select(candidate => candidate.PublicId)
                    .SingleAsync());

            Assert.True(retry.Success, retry.Message);
            Assert.Equal(QueuedMessageStatuses.Pending, retry.Payload!.Status);
            Assert.Equal(ServiceResponseType.NotFound, otherStore.ResponseType);
        }

        private static TransactionalMessageAdminService CreateService(
            CommerceNodeDbContext context,
            Guid storeId)
        {
            var audit = new Mock<IAdminAuditService>();
            audit
                .Setup(service => service.LogAsync(It.IsAny<CreateAdminAuditLogDto>()))
                .ReturnsAsync(new ServiceResponse<AdminAuditLogDto>(true, "Audit logged."));

            return new TransactionalMessageAdminService(
                context,
                new FixedStoreContext(storeId),
                new MessageTokenRenderer(),
                new MessageDeliveryService(
                    context,
                    Mock.Of<IStoreEmailTransportResolver>(),
                    Mock.Of<IStoreEmailTransportSender>()),
                audit.Object);
        }

        private static MessageTemplate CreateTemplate(Guid? storeId, string systemName)
        {
            return new MessageTemplate
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                SystemName = systemName,
                SubjectTemplate = "Default subject",
                BodyHtmlTemplate = "<p>Default body</p>",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
        }

        private static QueuedMessage CreateMessage(Guid storeId)
        {
            return new QueuedMessage
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                TemplateSystemName = TransactionalMessageTemplateSystemNames.PasswordRecovery,
                ToEmail = "customer@example.test",
                FromEmail = "sender@example.test",
                Subject = "Password reset",
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
                .UseInMemoryDatabase($"message-admin-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
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
