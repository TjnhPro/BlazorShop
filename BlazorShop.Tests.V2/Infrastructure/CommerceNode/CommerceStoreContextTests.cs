namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class CommerceStoreContextTests
    {
        [Fact]
        public async Task GetCurrentStoreIdAsync_WithoutExecutionContext_ReturnsValidation()
        {
            var context = new CommerceStoreContext(new StoreExecutionContextAccessor());

            var result = await context.GetCurrentStoreIdAsync();

            Assert.False(result.Success);
            Assert.Equal(ApplicationErrorKind.Validation, result.Error?.Kind);
        }

        [Fact]
        public async Task GetCurrentStoreIdAsync_WithActiveExecutionContext_ReturnsStoreId()
        {
            var storeId = Guid.NewGuid();
            var accessor = new StoreExecutionContextAccessor();
            accessor.SetCurrent(CreateExecutionContext(storeId, CommerceStoreStatuses.Active));
            var context = new CommerceStoreContext(accessor);

            var result = await context.GetCurrentStoreIdAsync();

            Assert.True(result.Success);
            Assert.Equal(storeId, result.Value);
        }

        [Fact]
        public async Task GetCurrentStoreIdAsync_WithDisabledExecutionContext_ReturnsNotFound()
        {
            var accessor = new StoreExecutionContextAccessor();
            accessor.SetCurrent(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Disabled));
            var context = new CommerceStoreContext(accessor);

            var result = await context.GetCurrentStoreIdAsync();

            Assert.False(result.Success);
            Assert.Equal(ApplicationErrorKind.NotFound, result.Error?.Kind);
        }

        [Fact]
        public async Task GetCurrentStoreAsync_WithDisabledExecutionContext_ReturnsReadinessStore()
        {
            var accessor = new StoreExecutionContextAccessor();
            accessor.SetCurrent(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Disabled));
            var context = new CommerceStoreContext(accessor);

            var result = await context.GetCurrentStoreAsync();

            Assert.True(result.Success);
            Assert.Equal(CommerceStoreStatuses.Disabled, result.Value?.Status);
        }

        private static StoreExecutionContext CreateExecutionContext(Guid storeId, string status)
        {
            return new StoreExecutionContext(
                storeId,
                "qa",
                "qa.example.test",
                StoreExecutionContextSources.StorefrontRoute,
                status,
                string.Equals(status, CommerceStoreStatuses.Active, StringComparison.OrdinalIgnoreCase),
                new CommerceCurrentStore(
                    Guid.NewGuid(),
                    "qa",
                    "QA Store",
                    status,
                    "https://qa.example.test",
                    "qa.example.test",
                    true,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "USD",
                    "en-US",
                    null,
                    null,
                    false,
                    null,
                    null));
        }
    }
}
