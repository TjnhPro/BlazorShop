extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Configuration;
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
        public async Task StorefrontMiddleware_ResolvesPublicMediaFromRequestHostAfterForwardedHeaders()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/media/products/9a50f55b-5b9d-4de8-b716-cc62f23c39bb";
            httpContext.Request.Host = new HostString("qa.example.test");
            httpContext.Request.Headers["X-Forwarded-Host"] = "forged.example.test";
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
        public async Task StorefrontMiddleware_PublicMediaIgnoresForgedStoreHostHeader()
        {
            var accessor = new StoreExecutionContextAccessor();
            var resolver = new CapturingResolver(CreateExecutionContext(Guid.NewGuid(), CommerceStoreStatuses.Active));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/media/products/9a50f55b-5b9d-4de8-b716-cc62f23c39bb";
            httpContext.Request.Host = new HostString("store-a.example.test");
            httpContext.Request.Headers["X-Store-Host"] = "store-b.example.test";
            var nextCalled = false;

            var middleware = new StorefrontStoreScopeMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(httpContext, resolver, accessor);

            Assert.True(nextCalled);
            Assert.Null(resolver.CapturedStoreKey);
            Assert.Equal("store-a.example.test", resolver.CapturedHost);
        }

        [Fact]
        public void CommerceNodeForwardedHeadersOptions_ReadTrustedProxyConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Runtime:ForwardedHeaders:KnownProxies:0"] = "10.0.0.20",
                    ["Runtime:ForwardedHeaders:KnownNetworks:0"] = "10.0.1.0/24",
                    ["Runtime:ForwardedHeaders:ForwardLimit"] = "1",
                })
                .Build();
            var options = new ForwardedHeadersOptions();

            new CommerceNodeForwardedHeadersOptionsSetup(configuration).Configure(options);

            Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
            Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
            Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
            Assert.Equal(1, options.ForwardLimit);
            Assert.Contains(IPAddress.Parse("10.0.0.20"), options.KnownProxies);
            Assert.Contains(options.KnownIPNetworks, network => network.PrefixLength == 24);
        }

        [Fact]
        public void CommerceNodeProgram_RunsForwardedHeadersBeforeStorefrontStoreScopeMiddleware()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Program.cs");
            var middleware = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Middleware/StorefrontStoreScopeMiddleware.cs");

            Assert.Contains("ConfigureOptions<CommerceNodeForwardedHeadersOptionsSetup>", program, StringComparison.Ordinal);
            Assert.True(
                program.IndexOf("app.UseForwardedHeaders();", StringComparison.Ordinal)
                < program.IndexOf("StorefrontStoreScopeMiddleware.IsStorefrontOrPublicMediaPath", StringComparison.Ordinal));
            Assert.DoesNotContain("X-Store-Host", middleware, StringComparison.Ordinal);
            Assert.DoesNotContain("X-Forwarded-Host", middleware, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }

            throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
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
