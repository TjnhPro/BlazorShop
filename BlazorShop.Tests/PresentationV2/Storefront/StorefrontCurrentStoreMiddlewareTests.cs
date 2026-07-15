extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontCurrentStoreMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenGuardDisabledInDevelopment_AllowsNextWithoutResolvingStore()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var provider = new StubCurrentStoreProvider(() => throw new InvalidOperationException("Provider should not be called."));
            var context = CreateContext("/new-releases");

            await middleware.InvokeAsync(
                context,
                provider,
                Options.Create(new StorefrontStoreResolutionOptions()),
                CreateEnvironment("Development"),
                CreateConfiguration());

            Assert.True(nextCalled);
            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public async Task InvokeAsync_WhenStoreNotFound_Returns404AndStopsPipeline()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var provider = new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.NotFound);
            var context = CreateContext("/new-releases");

            await middleware.InvokeAsync(
                context,
                provider,
                Options.Create(new StorefrontStoreResolutionOptions { RequireCurrentStore = true }),
                CreateEnvironment("Development"),
                CreateConfiguration());

            Assert.False(nextCalled);
            Assert.Equal(1, provider.CallCount);
            Assert.Equal(HttpStatusCode.NotFound, (HttpStatusCode)context.Response.StatusCode);
            Assert.Contains("Storefront store was not found", await ReadBodyAsync(context));
        }

        [Fact]
        public async Task InvokeAsync_WhenCommerceNodeUnavailable_Returns503AndStopsPipeline()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var provider = new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.ServiceUnavailable);
            var context = CreateContext("/checkout");

            await middleware.InvokeAsync(
                context,
                provider,
                Options.Create(new StorefrontStoreResolutionOptions { RequireCurrentStore = true }),
                CreateEnvironment("Development"),
                CreateConfiguration());

            Assert.False(nextCalled);
            Assert.Equal(1, provider.CallCount);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (HttpStatusCode)context.Response.StatusCode);
            Assert.Contains("configured store could not be resolved", await ReadBodyAsync(context));
        }

        [Fact]
        public async Task InvokeAsync_WhenStaticAssetPath_SkipsCurrentStoreGuard()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var provider = new StubCurrentStoreProvider(() => throw new InvalidOperationException("Provider should not be called."));
            var context = CreateContext("/css/app.css");

            await middleware.InvokeAsync(
                context,
                provider,
                Options.Create(new StorefrontStoreResolutionOptions { RequireCurrentStore = true }),
                CreateEnvironment("Production"),
                CreateConfiguration());

            Assert.True(nextCalled);
            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public async Task InvokeAsync_WhenStoreInMaintenance_Returns503()
        {
            var middleware = CreateMiddleware(_ => Task.CompletedTask);
            var provider = new StubCurrentStoreProvider(() => StorefrontCurrentStoreResolution.Maintenance(CreateCurrentStore()));
            var context = CreateContext("/sitemap.xml");

            await middleware.InvokeAsync(
                context,
                provider,
                Options.Create(new StorefrontStoreResolutionOptions { RequireCurrentStore = true }),
                CreateEnvironment("Production"),
                CreateConfiguration());

            Assert.Equal(1, provider.CallCount);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (HttpStatusCode)context.Response.StatusCode);
            Assert.Contains("Maintenance window.", await ReadBodyAsync(context));
        }

        private static StorefrontCurrentStoreMiddleware CreateMiddleware(RequestDelegate next)
        {
            return new StorefrontCurrentStoreMiddleware(
                next,
                NullLogger<StorefrontCurrentStoreMiddleware>.Instance);
        }

        private static DefaultHttpContext CreateContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:StoreKey"] = "default",
                })
                .Build();
        }

        private static IHostEnvironment CreateEnvironment(string environmentName)
        {
            return new TestHostEnvironment
            {
                EnvironmentName = environmentName,
            };
        }

        private static async Task<string> ReadBodyAsync(HttpContext context)
        {
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private static StorefrontCurrentStore CreateCurrentStore()
        {
            return new StorefrontCurrentStore(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "default",
                "Default Store",
                "Active",
                "https://store.example/",
                "store.example",
                true,
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
                true,
                "Maintenance window.",
                null);
        }

        private sealed class StubCurrentStoreProvider : IStorefrontCurrentStoreProvider
        {
            private readonly Func<StorefrontCurrentStoreResolution> _resolutionFactory;

            public StubCurrentStoreProvider(Func<StorefrontCurrentStoreResolution> resolutionFactory)
            {
                _resolutionFactory = resolutionFactory;
            }

            public int CallCount { get; private set; }

            public Task<StorefrontCurrentStoreResolution> ResolveAsync(CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(_resolutionFactory());
            }
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;

            public string ApplicationName { get; set; } = "BlazorShop.Tests";

            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
