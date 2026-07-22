extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontCurrentStoreMiddlewareRegressionTests
    {
        [Fact]
        public void DevelopmentConfiguration_RequiresCurrentStoreGuard()
        {
            var repositoryRoot = FindRepositoryRoot();
            var storefrontRoot = Path.Combine(
                repositoryRoot,
                "BlazorShop.PresentationV2",
                "BlazorShop.Storefront.V2");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(storefrontRoot)
                .AddJsonFile("appsettings.Development.json", optional: false)
                .Build();
            var localEnv = ReadEnvFile(Path.Combine(repositoryRoot, "scripts", "env", "v2-local.env"));

            Assert.Equal("default", configuration["Api:StoreKey"]);
            Assert.True(bool.TryParse(configuration["StoreResolution:RequireCurrentStore"], out var requireCurrentStore));
            Assert.True(requireCurrentStore);
            Assert.Equal("true", localEnv["STOREFRONT_V2__StoreResolution__RequireCurrentStore"]);
            Assert.True(StorefrontStoreResolutionOptions.IsCurrentStoreRequired(
                new StorefrontStoreResolutionOptions { RequireCurrentStore = requireCurrentStore },
                new StubEnvironment { EnvironmentName = "Development" }));
        }

        [Fact]
        public async Task HtmlUnavailableRedirect_RemainsRedirectWhenResponseStarts()
        {
            // Regression: ISSUE-001 - service-unavailable headers changed HTML redirect into HTTP 503.
            // Found by /qa on 2026-07-15.
            // Report: .gstack/qa-reports/qa-report-store-lifecycle-2026-07-15.md
            var middleware = new StorefrontCurrentStoreMiddleware(
                _ => Task.CompletedTask,
                NullLogger<StorefrontCurrentStoreMiddleware>.Instance);
            var provider = new StubCurrentStoreProvider(
                StorefrontCurrentStoreResolution.Closed(CreateCurrentStore()));
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/";
            context.Request.Headers.Accept = "text/html";
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(
                context,
                provider,
                Options.Create(new StorefrontStoreResolutionOptions { RequireCurrentStore = true }),
                new StubEnvironment(),
                new ConfigurationBuilder().Build());
            await context.Response.StartAsync();

            Assert.Equal(HttpStatusCode.Redirect, (HttpStatusCode)context.Response.StatusCode);
            Assert.Equal("/maintenance?reason=closed", context.Response.Headers.Location);
            Assert.Equal(StorefrontResponseHeaders.NoIndexNoFollow, context.Response.Headers["X-Robots-Tag"]);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
        }

        private static IReadOnlyDictionary<string, string> ReadEnvFile(string path)
        {
            return File.ReadLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private static StorefrontCurrentStore CreateCurrentStore()
        {
            return new StorefrontCurrentStore(
                Guid.NewGuid(),
                "default",
                "Default Store",
                "disabled",
                "https://shop.example.test",
                "shop.example.test",
                false,
                null,
                null,
                "Example Company",
                "company@example.test",
                "+1-555-0100",
                "1 Test Street",
                null,
                null,
                null,
                null,
                null,
                "USD",
                "en-US",
                "support@example.test",
                "+1-555-0199",
                false,
                null,
                null);
        }

        private sealed class StubCurrentStoreProvider : IStorefrontCurrentStoreProvider
        {
            private readonly StorefrontCurrentStoreResolution resolution;

            public StubCurrentStoreProvider(StorefrontCurrentStoreResolution resolution)
            {
                this.resolution = resolution;
            }

            public Task<StorefrontCurrentStoreResolution> ResolveAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.resolution);
            }
        }

        private sealed class StubEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Production";

            public string ApplicationName { get; set; } = "BlazorShop.Storefront.V2";

            public string WebRootPath { get; set; } = string.Empty;

            public IFileProvider WebRootFileProvider { get; set; } =
                new NullFileProvider();

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();
        }
    }
}
