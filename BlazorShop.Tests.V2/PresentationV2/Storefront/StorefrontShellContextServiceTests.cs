extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging.Abstractions;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Presentation.Models;
    using StorefrontV2::BlazorShop.Storefront.Presentation.Services;
    using StorefrontV2::BlazorShop.Storefront.Presentation.Contracts;

    public sealed class StorefrontShellContextServiceTests
    {
        [Fact]
        public async Task GetAsync_LoadsDataOncePerRequest()
        {
            var dependencies = new Dependencies();
            var service = dependencies.CreateService("/checkout", "?step=review");

            var first = await service.GetAsync();
            var second = await service.GetAsync();

            Assert.Same(first, second);
            Assert.Equal(1, dependencies.DisplayProvider.CallCount);
            Assert.Equal(1, dependencies.CatalogClient.CategoryTreeCalls);
            Assert.Equal(4, dependencies.PageNavigationProvider.LocationCalls);
            Assert.Equal(4, dependencies.NavigationProvider.MenuCalls);
            Assert.Equal(1, dependencies.ConfigurationClient.PublicConfigurationCalls);
            Assert.Equal(1, dependencies.SessionResolver.CallCount);
            Assert.Equal("/checkout?step=review", first.ReturnUrl);
        }

        [Fact]
        public async Task GetAsync_WhenAnonymous_BuildsAnonymousAccountContext()
        {
            var dependencies = new Dependencies();
            dependencies.SessionResolver.Session = StorefrontSessionInfo.Anonymous;

            var context = await dependencies.CreateService("/").GetAsync();

            Assert.False(context.AccountMenu.IsAuthenticated);
            Assert.Contains(context.AccountMenu.Links, link => link.Label == "Sign in");
            Assert.Contains(context.AccountMenu.Links, link => link.Label == "Register");
        }

        [Fact]
        public async Task GetAsync_SortsAndFiltersSearchCategories()
        {
            var dependencies = new Dependencies();
            dependencies.CatalogClient.Categories =
            [
                new GetCategoryTreeNode { Name = "Zeta", Slug = "zeta", Children = [] },
                new GetCategoryTreeNode { Name = " ", Slug = "bad", Children = [] },
                new GetCategoryTreeNode
                {
                    Name = "Alpha",
                    Slug = "alpha",
                    Children =
                    [
                        new GetCategoryTreeNode { Name = "Beta", Slug = "beta", Children = [] },
                    ],
                },
            ];

            var context = await dependencies.CreateService("/search").GetAsync();

            Assert.Equal(["Alpha", "-- Beta", "Zeta"], context.Search.Categories.Select(category => category.Label).ToArray());
            Assert.All(context.Search.Categories, category => Assert.DoesNotContain("://", category.Href, StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetAsync_BuildsCurrencyContextFromDisplayContext()
        {
            var dependencies = new Dependencies();
            dependencies.DisplayProvider.Display = dependencies.DisplayProvider.Display with
            {
                CurrencyCode = "EUR",
                DefaultCurrencyCode = "USD",
                SupportedCurrencyCodes = ["USD", "EUR"],
            };

            var context = await dependencies.CreateService("/cart").GetAsync();

            Assert.True(context.Currency.ShowSelector);
            Assert.Equal("EUR", context.Currency.CurrentCurrencyCode);
            Assert.Equal("USD", context.Currency.DefaultCurrencyCode);
            Assert.Equal(["USD", "EUR"], context.Currency.SupportedCurrencyCodes);
            Assert.Equal("/cart", context.Currency.ReturnUrl);
        }

        [Fact]
        public async Task GetAsync_ReturnUrlFallsBackWhenHttpContextMissing()
        {
            var dependencies = new Dependencies();
            var service = dependencies.CreateServiceWithoutHttpContext();

            var context = await service.GetAsync();

            Assert.Equal("/", context.ReturnUrl);
        }

        [Fact]
        public async Task GetAsync_WhenRequestPathLooksExternal_FallsBackToHome()
        {
            var dependencies = new Dependencies();
            var service = dependencies.CreateService("//evil.example/path", "?x=1");

            var context = await service.GetAsync();

            Assert.Equal("/", context.ReturnUrl);
            Assert.Equal("/", context.Currency.ReturnUrl);
        }

        [Fact]
        public async Task GetAsync_WhenOptionalMenuFails_ReturnsFallbackMenuState()
        {
            var dependencies = new Dependencies();
            dependencies.NavigationProvider.ThrowForSystemName = StoreNavigationMenuNames.Main;
            dependencies.PageNavigationProvider.ThrowForLocation = StorefrontPageContentRules.FooterSupport;
            dependencies.CatalogClient.ThrowOnCategoryTree = true;

            var context = await dependencies.CreateService("/").GetAsync();

            Assert.Null(context.Navigation.MainMenu);
            Assert.Empty(context.Navigation.FooterSupportLinks);
            Assert.Empty(context.Search.Categories);
            Assert.NotNull(context.Header);
            Assert.Equal("Demo Co", context.Brand.Name);
        }

        [Fact]
        public async Task GetAsync_WhenRequiredDisplayFails_PropagatesFailure()
        {
            var dependencies = new Dependencies();
            dependencies.DisplayProvider.ThrowOnLoad = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => dependencies.CreateService("/").GetAsync());
        }

        private sealed class Dependencies
        {
            private readonly DefaultHttpContext _httpContext = new();

            public StubDisplayContextProvider DisplayProvider { get; } = new();

            public StubCatalogClient CatalogClient { get; } = new();

            public StubPageNavigationProvider PageNavigationProvider { get; } = new();

            public StubNavigationProvider NavigationProvider { get; } = new();

            public StubConfigurationClient ConfigurationClient { get; } = new();

            public StubSessionResolver SessionResolver { get; } = new();

            public StorefrontShellContextService CreateService(string path, string queryString = "")
            {
                _httpContext.Request.Path = path;
                _httpContext.Request.QueryString = new QueryString(queryString);
                return CreateServiceCore(new HttpContextAccessor { HttpContext = _httpContext });
            }

            public StorefrontShellContextService CreateServiceWithoutHttpContext()
            {
                return CreateServiceCore(new HttpContextAccessor());
            }

            private StorefrontShellContextService CreateServiceCore(IHttpContextAccessor httpContextAccessor)
            {
                return new StorefrontShellContextService(
                    DisplayProvider,
                    CatalogClient,
                    PageNavigationProvider,
                    NavigationProvider,
                    ConfigurationClient,
                    SessionResolver,
                    httpContextAccessor,
                    NullLogger<StorefrontShellContextService>.Instance);
            }
        }

        private sealed class StubDisplayContextProvider : IStorefrontDisplayContextProvider
        {
            public StorefrontDisplayContext Display { get; set; } = StorefrontDisplayContext.Fallback with
            {
                StoreName = "Demo Store",
                CompanyName = "Demo Co",
                CompanyEmail = "hello@example.test",
                CompanyPhone = "+1 555 0100",
                CompanyAddress = "1 Test Street",
                SupportEmail = "support@example.test",
                SupportedCurrencyCodes = ["USD"],
            };

            public int CallCount { get; private set; }

            public bool ThrowOnLoad { get; set; }

            public Task<StorefrontDisplayContext> GetAsync(CancellationToken cancellationToken = default)
            {
                CallCount++;
                if (ThrowOnLoad)
                {
                    throw new InvalidOperationException("Display context failed.");
                }

                return Task.FromResult(Display);
            }
        }

        private sealed class StubCatalogClient : IStorefrontCatalogClient
        {
            public IReadOnlyList<GetCategoryTreeNode> Categories { get; set; } =
            [
                new GetCategoryTreeNode { Name = "Default", Slug = "default", Children = [] },
            ];

            public int CategoryTreeCalls { get; private set; }

            public bool ThrowOnCategoryTree { get; set; }

            public Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
            {
                CategoryTreeCalls++;
                if (ThrowOnCategoryTree)
                {
                    throw new InvalidOperationException("Category tree failed.");
                }

                return Task.FromResult(StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.Success(Categories));
            }

            public Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, string? currencyCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, string? currencyCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(string? categorySlug = null, string? searchTerm = null, string? currencyCode = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(string? searchTerm, string? categorySlug = null, int? limit = null, string? currencyCode = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, string? currencyCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, string? currencyCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(Guid productId, StorefrontProductSelectionPreviewRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class StubPageNavigationProvider : IStorefrontPageNavigationProvider
        {
            public int LocationCalls { get; private set; }

            public string? ThrowForLocation { get; set; }

            public Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksByLocationAsync(
                string navigationLocation,
                CancellationToken cancellationToken = default)
            {
                LocationCalls++;
                if (string.Equals(ThrowForLocation, navigationLocation, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Page navigation failed.");
                }

                IReadOnlyList<StorefrontPageNavigationLinkDto> links =
                [
                    new(navigationLocation, $"{navigationLocation}-page", $"{navigationLocation} page", navigationLocation, 10),
                ];
                return Task.FromResult(links);
            }
        }

        private sealed class StubNavigationProvider : IStorefrontNavigationProvider
        {
            public int MenuCalls { get; private set; }

            public string? ThrowForSystemName { get; set; }

            public Task<StoreNavigationPublicMenuDto?> GetMenuAsync(string systemName, CancellationToken cancellationToken = default)
            {
                MenuCalls++;
                if (string.Equals(ThrowForSystemName, systemName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Navigation failed.");
                }

                return Task.FromResult<StoreNavigationPublicMenuDto?>(new StoreNavigationPublicMenuDto(
                    systemName,
                    DateTimeOffset.UtcNow,
                    [new StoreNavigationPublicItemDto($"{systemName} link", $"/{systemName}", "url", null, false, [])]));
            }
        }

        private sealed class StubConfigurationClient : IStorefrontStoreConfigurationClient
        {
            public int PublicConfigurationCalls { get; private set; }

            public Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
            {
                PublicConfigurationCalls++;
                return Task.FromResult(StorefrontApiResult<StorefrontPublicConfiguration>.Success(
                    new StorefrontPublicConfiguration(
                        new StorefrontStoreIdentity(Guid.NewGuid(), "demo", "Demo Store", "Active", null, null, false),
                        new StorefrontBranding(null, null, "Demo Co", "hello@example.test", "+1 555 0100", "1 Test Street", null, null, null, null, null, "support@example.test", null, null),
                        new StorefrontLocaleOptions("en-US", ["en-US"]),
                        new StorefrontCurrencyOptions("USD", ["USD"]),
                        new StorefrontConsentConfiguration(true, true, "v1", "/pages/cookies", [], 180),
                        new StorefrontCaptchaConfiguration(false, string.Empty, null, [], new Dictionary<string, string>(StringComparer.Ordinal)),
                        new StorefrontMaintenanceState(false, null),
                        new StorefrontFeatureFlags(true, true, true, true, true, true),
                        new Dictionary<string, StorefrontCapability>(StringComparer.Ordinal),
                        [],
                        new StorefrontSeoDefaults(null, null, null, null, null, null, null, null, null, null, null, null, null))));
            }

            public Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
                StorefrontCurrencyPreferenceRequest request,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private sealed class StubSessionResolver : IStorefrontSessionResolver
        {
            public StorefrontSessionInfo Session { get; set; } = new(true, false, "Ada", "ada@example.test");

            public int CallCount { get; private set; }

            public Task<StorefrontSessionInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(Session);
            }
        }

    }
}
