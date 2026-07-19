extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.AspNetCore.Http;

    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Middleware;

    public sealed class StoreExecutionContextMiddlewareTests
    {
        [Fact]
        public async Task StorefrontMiddleware_ResolvesStorefrontRouteStoreKey()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/storefront/stores/qa/cart";
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                Assert.Equal("qa", accessor.Current?.StoreKey);
                Assert.Equal(StoreExecutionContextSources.StorefrontRoute, accessor.Current?.Source);
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.True(nextCalled);
            Assert.Equal("qa", resolver.CapturedStoreKey);
            Assert.Null(resolver.CapturedHost);
        }

        [Fact]
        public async Task CommerceAdminMiddleware_ResolvesStoreScopedQueryAndIgnoresStoreKeyHeader()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/commerce/admin/products";
            httpContext.Request.QueryString = new QueryString("?storeKey=qa");
            httpContext.Request.Headers["X-Store-Key"] = "wrong";
            var nextCalled = false;

            var middleware = new CommerceAdminStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                Assert.Equal(StoreExecutionContextSources.CommerceAdminQuery, accessor.Current?.Source);
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.True(nextCalled);
            Assert.Equal("qa", resolver.CapturedStoreKey);
            Assert.Null(resolver.CapturedHost);
        }

        [Fact]
        public async Task CommerceAdminMiddleware_RejectsMissingStoreKeyBeforeNext()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/commerce/admin/products";
            var nextCalled = false;

            var middleware = new CommerceAdminStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
            Assert.Null(accessor.Current);
        }

        [Fact]
        public async Task StorefrontMiddleware_ResolvesPublicMediaFromForwardedHost()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/media/products/9a50f55b-5b9d-4de8-b716-cc62f23c39bb";
            httpContext.Request.Headers["X-Forwarded-Host"] = "qa.example.test";
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                Assert.Equal(StoreExecutionContextSources.PublicMediaHost, accessor.Current?.Source);
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.True(nextCalled);
            Assert.Null(resolver.CapturedStoreKey);
            Assert.Equal("qa.example.test", resolver.CapturedHost);
        }

        [Fact]
        public async Task StorefrontMiddleware_MissingRouteStoreKeyDoesNotSetExecutionContext()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/storefront/stores";
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                Assert.Null(accessor.Current);
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.True(nextCalled);
            Assert.Null(resolver.CapturedStoreKey);
            Assert.Null(resolver.CapturedHost);
        }

        [Fact]
        public async Task StorefrontMiddleware_PublicMediaUnknownHostDoesNotFallback()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(
                ApplicationResult<StoreExecutionContext>.Failed(
                    ApplicationError.NotFound("store.not_found", "Store host was not found.")));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/media/assets/9a50f55b-5b9d-4de8-b716-cc62f23c39bb";
            httpContext.Request.Host = new HostString("unknown.example.test");
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
            Assert.Null(accessor.Current);
            Assert.Equal("unknown.example.test", resolver.CapturedHost);
        }

        [Fact]
        public async Task StorefrontMiddleware_PublicMediaMissingHostDoesNotFallbackToSingleStore()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/media/products/9a50f55b-5b9d-4de8-b716-cc62f23c39bb";
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
            Assert.Null(accessor.Current);
            Assert.Null(resolver.CapturedHost);
        }

        [Fact]
        public async Task StorefrontMiddleware_DoesNotFallbackWhenStoreCannotResolve()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(
                ApplicationResult<StoreExecutionContext>.Failed(
                    ApplicationError.NotFound("store.not_found", "Store was not found.")));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/storefront/stores/missing/cart";
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
            Assert.Null(accessor.Current);
        }

        private static StoreExecutionContext CreateExecutionContext(Guid storeId, string status)
        {
            return new StoreExecutionContext(
                storeId,
                "qa",
                "qa.example.test",
                StoreExecutionContextSources.Unknown,
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

        private sealed class CapturingResolver : ICommerceStoreDomainResolver
        {
            private readonly ApplicationResult<StoreExecutionContext> executionContextResult;

            public CapturingResolver(StoreExecutionContext executionContext)
                : this(ApplicationResult<StoreExecutionContext>.Succeeded(executionContext))
            {
            }

            public CapturingResolver(ApplicationResult<StoreExecutionContext> executionContextResult)
            {
                this.executionContextResult = executionContextResult;
            }

            public string? CapturedStoreKey { get; private set; }

            public string? CapturedHost { get; private set; }

            public string? CapturedSource { get; private set; }

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ApplicationResult<StoreExecutionContext>> ResolveExecutionContextAsync(
                string? storeKey = null,
                string? host = null,
                string source = StoreExecutionContextSources.Unknown,
                CancellationToken cancellationToken = default)
            {
                this.CapturedStoreKey = storeKey;
                this.CapturedHost = host;
                this.CapturedSource = source;
                return Task.FromResult(this.executionContextResult.Success && this.executionContextResult.Value is not null
                    ? ApplicationResult<StoreExecutionContext>.Succeeded(this.executionContextResult.Value with { Source = source, Host = host })
                    : this.executionContextResult);
            }

            public Task<ApplicationResult<Guid>> ResolveStoreIdAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
