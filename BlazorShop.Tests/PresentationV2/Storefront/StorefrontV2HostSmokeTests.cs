extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    using StorefrontV2Program = StorefrontV2::Program;

    public sealed class StorefrontV2HostSmokeTests : IClassFixture<WebApplicationFactory<StorefrontV2Program>>
    {
        private readonly WebApplicationFactory<StorefrontV2Program> _factory;

        public StorefrontV2HostSmokeTests(WebApplicationFactory<StorefrontV2Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Checkout_RedirectsAnonymousCustomer_ToClientAppCheckoutLogin()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontClientAppUrlResolver>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontClientAppUrlResolver>(_ => new StubStorefrontClientAppUrlResolver("https://account.example.com/"));
                },
                allowAutoRedirect: false);

            using var response = await client.GetAsync(StorefrontRoutes.Checkout);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://account.example.com/authentication/login/account/checkout", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Robots_ReturnsTextDocument()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontRobotsService>();
                services.AddScoped<IStorefrontRobotsService>(_ => new StubRobotsService("User-agent: *\nAllow: /\n"));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Robots);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("User-agent: *", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Sitemap_ReturnsXmlDocument()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSitemapService>();
                services.AddScoped<IStorefrontSitemapService>(_ => new StubSitemapService(StorefrontSitemapGenerationResult.Success("<urlset />")));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Sitemap);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("<urlset />", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Cart_RendersEmptyCartWithoutCommerceNode()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new ServiceUnavailableHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Cart);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("cart", content, StringComparison.OrdinalIgnoreCase);
        }

        private HttpClient CreateClient(Action<IServiceCollection> configureServices, bool allowAutoRedirect = true)
        {
            var configuredFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(configureServices);
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect,
            });
        }

        private sealed class StubStorefrontSessionResolver : IStorefrontSessionResolver
        {
            private readonly StorefrontSessionInfo _sessionInfo;

            public StubStorefrontSessionResolver(StorefrontSessionInfo sessionInfo)
            {
                _sessionInfo = sessionInfo;
            }

            public Task<StorefrontSessionInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_sessionInfo);
            }
        }

        private sealed class StubStorefrontClientAppUrlResolver : IStorefrontClientAppUrlResolver
        {
            private readonly string _baseUrl;

            public StubStorefrontClientAppUrlResolver(string baseUrl)
            {
                _baseUrl = baseUrl;
            }

            public string ResolveBaseUrl()
            {
                return _baseUrl;
            }

            public string ResolveUrl(string? path)
            {
                return $"{_baseUrl.TrimEnd('/')}/{(path ?? string.Empty).TrimStart('/')}";
            }
        }

        private sealed class StubRobotsService : IStorefrontRobotsService
        {
            private readonly string _content;

            public StubRobotsService(string content)
            {
                _content = content;
            }

            public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_content);
            }
        }

        private sealed class StubSitemapService : IStorefrontSitemapService
        {
            private readonly StorefrontSitemapGenerationResult _result;

            public StubSitemapService(StorefrontSitemapGenerationResult result)
            {
                _result = result;
            }

            public Task<StorefrontSitemapGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_result);
            }
        }

        private sealed class ServiceUnavailableHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
        }
    }
}
