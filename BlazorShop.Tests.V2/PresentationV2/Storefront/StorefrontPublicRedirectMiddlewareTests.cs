extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging.Abstractions;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Models;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontPublicRedirectMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenRedirectIsValid_ReturnsConfiguredRedirect()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var client = new StubContentClient(StorefrontApiResult<SeoRedirectResolutionDto>.Success(new SeoRedirectResolutionDto
            {
                NewPath = "/new-path",
                StatusCode = StatusCodes.Status308PermanentRedirect,
            }));
            var context = CreateContext("/old-path");

            await middleware.InvokeAsync(context, client);

            Assert.False(nextCalled);
            Assert.Equal(HttpStatusCode.PermanentRedirect, (HttpStatusCode)context.Response.StatusCode);
            Assert.Equal("/new-path", context.Response.Headers.Location);
            Assert.Equal("/old-path", client.LastRedirectPath);
        }

        [Theory]
        [InlineData("https://evil.example/path")]
        [InlineData("//evil.example/path")]
        [InlineData("/new-path\r\nx-injected: true")]
        public async Task InvokeAsync_WhenRedirectTargetIsInvalid_FallsThrough(string newPath)
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var client = new StubContentClient(StorefrontApiResult<SeoRedirectResolutionDto>.Success(new SeoRedirectResolutionDto
            {
                NewPath = newPath,
                StatusCode = StatusCodes.Status308PermanentRedirect,
            }));
            var context = CreateContext("/old-path");

            await middleware.InvokeAsync(context, client);

            Assert.True(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            AssertNoLocationHeader(context);
        }

        [Fact]
        public async Task InvokeAsync_WhenRedirectLoops_FallsThrough()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var client = new StubContentClient(StorefrontApiResult<SeoRedirectResolutionDto>.Success(new SeoRedirectResolutionDto
            {
                NewPath = "/old-path",
                StatusCode = StatusCodes.Status308PermanentRedirect,
            }));
            var context = CreateContext("/old-path");

            await middleware.InvokeAsync(context, client);

            Assert.True(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            AssertNoLocationHeader(context);
        }

        [Fact]
        public async Task InvokeAsync_WhenRedirectMissing_FallsThrough()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var client = new StubContentClient(StorefrontApiResult<SeoRedirectResolutionDto>.NotFound());
            var context = CreateContext("/old-path");

            await middleware.InvokeAsync(context, client);

            Assert.True(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WhenPathIsStaticAsset_SkipsRedirectResolution()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var client = new StubContentClient(() => throw new InvalidOperationException("Redirect API should not be called."));
            var context = CreateContext("/css/app.css");

            await middleware.InvokeAsync(context, client);

            Assert.True(nextCalled);
            Assert.Null(client.LastRedirectPath);
        }

        private static StorefrontPublicRedirectMiddleware CreateMiddleware(RequestDelegate next)
        {
            return new StorefrontPublicRedirectMiddleware(
                next,
                NullLogger<StorefrontPublicRedirectMiddleware>.Instance);
        }

        private static DefaultHttpContext CreateContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static void AssertNoLocationHeader(HttpContext context)
        {
            Assert.False(context.Response.Headers.TryGetValue("Location", out var location), location.ToString());
        }

        private sealed class StubContentClient : IStorefrontContentClient
        {
            private readonly Func<StorefrontApiResult<SeoRedirectResolutionDto>> _redirectFactory;

            public StubContentClient(StorefrontApiResult<SeoRedirectResolutionDto> redirectResult)
                : this(() => redirectResult)
            {
            }

            public StubContentClient(Func<StorefrontApiResult<SeoRedirectResolutionDto>> redirectFactory)
            {
                _redirectFactory = redirectFactory;
            }

            public string? LastRedirectPath { get; private set; }

            public Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(
                string slug,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
                string systemName,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(
                string path,
                CancellationToken cancellationToken = default)
            {
                LastRedirectPath = path;
                return Task.FromResult(_redirectFactory());
            }
        }
    }
}
