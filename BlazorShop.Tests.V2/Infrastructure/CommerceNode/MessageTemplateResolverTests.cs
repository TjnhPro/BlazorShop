namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class MessageTemplateResolverTests
    {
        [Fact]
        public async Task ResolveAsync_PrefersStoreAndLanguageOverrideBeforeGlobalDefault()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.MessageTemplates.AddRange(
                CreateTemplate(TransactionalMessageTemplateSystemNames.OrderPlaced, null, null, "Global default"),
                CreateTemplate(TransactionalMessageTemplateSystemNames.OrderPlaced, null, "vi", "Global vi"),
                CreateTemplate(TransactionalMessageTemplateSystemNames.OrderPlaced, storeId, null, "Store default"),
                CreateTemplate(TransactionalMessageTemplateSystemNames.OrderPlaced, storeId, "vi", "Store vi"));
            await context.SaveChangesAsync();
            var resolver = new MessageTemplateResolver(context);

            var result = await resolver.ResolveAsync(new MessageTemplateResolutionRequest(
                TransactionalMessageTemplateSystemNames.OrderPlaced,
                storeId,
                "vi"));

            Assert.True(result.Success);
            Assert.Equal("Store vi", result.Template!.SubjectTemplate);
        }

        [Fact]
        public async Task ResolveAsync_FallsBackToGlobalDefaultWhenStoreTemplateMissing()
        {
            await using var context = CreateContext();
            context.MessageTemplates.Add(CreateTemplate(
                TransactionalMessageTemplateSystemNames.PasswordRecovery,
                null,
                null,
                "Global reset"));
            await context.SaveChangesAsync();
            var resolver = new MessageTemplateResolver(context);

            var result = await resolver.ResolveAsync(new MessageTemplateResolutionRequest(
                TransactionalMessageTemplateSystemNames.PasswordRecovery,
                Guid.NewGuid(),
                "fr"));

            Assert.True(result.Success);
            Assert.Equal("Global reset", result.Template!.SubjectTemplate);
        }

        [Fact]
        public async Task ResolveAsync_IgnoresInactiveTemplates()
        {
            await using var context = CreateContext();
            context.MessageTemplates.Add(CreateTemplate(
                TransactionalMessageTemplateSystemNames.AccountActivation,
                null,
                null,
                "Inactive",
                isActive: false));
            await context.SaveChangesAsync();
            var resolver = new MessageTemplateResolver(context);

            var result = await resolver.ResolveAsync(new MessageTemplateResolutionRequest(
                TransactionalMessageTemplateSystemNames.AccountActivation));

            Assert.False(result.Success);
            Assert.Equal("message_template.not_found", result.ErrorCode);
        }

        private static MessageTemplate CreateTemplate(
            string systemName,
            Guid? storeId,
            string? languageCode,
            string subject,
            bool isActive = true)
        {
            return new MessageTemplate
            {
                SystemName = systemName,
                StoreId = storeId,
                LanguageCode = languageCode,
                SubjectTemplate = subject,
                BodyHtmlTemplate = "<p>{{Store.Name}}</p>",
                IsActive = isActive,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"message-template-resolver-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
