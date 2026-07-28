extern alias Starter;

namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Net;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Runtime;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using StarterProgram = Starter::BlazorShop.Storefront.Starter.Program;
using RuntimeCatalogPagedResponse = BlazorShop.Storefront.Client.StorefrontCatalogProductResponseStorefrontPagedResponse;
using RuntimeCategoryPageResponse = BlazorShop.Storefront.Client.StorefrontCategoryPageResponse;
using RuntimeCategoryResponse = BlazorShop.Storefront.Client.StorefrontCategoryResponse;
using RuntimeCategoryTreeNodeResponse = BlazorShop.Storefront.Client.StorefrontCategoryTreeNodeResponse;
using RuntimeAddressConfigurationResponse = BlazorShop.Storefront.Client.StorefrontAddressFieldConfigurationResponse;
using RuntimeAddressCountryResponse = BlazorShop.Storefront.Client.StorefrontAddressCountryResponse;
using RuntimeAddressStateProvinceResponse = BlazorShop.Storefront.Client.StorefrontAddressStateProvinceResponse;
using RuntimeCheckoutAddressStepRequest = BlazorShop.Storefront.Client.StorefrontCheckoutAddressStepRequest;
using RuntimeCheckoutPaymentMethodRequest = BlazorShop.Storefront.Client.StorefrontCheckoutPaymentMethodRequest;
using RuntimeCheckoutPaymentMethodResponse = BlazorShop.Storefront.Client.StorefrontCheckoutPaymentMethodOptionResponse;
using RuntimeCheckoutPreviewRequest = BlazorShop.Storefront.Client.StorefrontCheckoutPreviewRequest;
using RuntimeCheckoutPreviewResponse = BlazorShop.Storefront.Client.StorefrontCheckoutPreviewResponse;
using RuntimeCheckoutReviewRequest = BlazorShop.Storefront.Client.StorefrontCheckoutReviewRequest;
using RuntimeCheckoutReviewResponse = BlazorShop.Storefront.Client.StorefrontCheckoutReviewResponse;
using RuntimeCheckoutSessionResponse = BlazorShop.Storefront.Client.StorefrontCheckoutSessionResponse;
using RuntimeCheckoutShippingMethodRequest = BlazorShop.Storefront.Client.StorefrontCheckoutShippingMethodRequest;
using RuntimePaymentAttemptResponse = BlazorShop.Storefront.Client.StorefrontPaymentAttemptResponse;
using RuntimePaymentMethodResponse = BlazorShop.Storefront.Client.StorefrontPaymentMethodResponse;
using RuntimePlaceOrderRequest = BlazorShop.Storefront.Client.StorefrontPlaceOrderRequest;
using RuntimePlaceOrderResponse = BlazorShop.Storefront.Client.StorefrontPlaceOrderResponse;
using RuntimePublicCatalogSitemap = BlazorShop.Storefront.Client.GetPublicCatalogSitemap;
using RuntimeProductFilterMetadataResponse = BlazorShop.Storefront.Client.StorefrontProductFilterMetadataResponse;
using RuntimeProductResponse = BlazorShop.Storefront.Client.StorefrontProductResponse;
using RuntimeProductSelectionPreviewRequest = BlazorShop.Storefront.Client.StorefrontProductSelectionPreviewRequest;
using RuntimeProductSelectionPreviewResponse = BlazorShop.Storefront.Client.StorefrontProductSelectionPreviewResponse;
using RuntimeSearchSuggestionResponse = BlazorShop.Storefront.Client.StorefrontSearchSuggestionResponse;

public sealed class StorefrontStarterHostSmokeTests : IClassFixture<WebApplicationFactory<StarterProgram>>
{
    private readonly WebApplicationFactory<StarterProgram> factory;

    public StorefrontStarterHostSmokeTests(WebApplicationFactory<StarterProgram> factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("/", "Starter Fixture Store", "Starter Smoke Product")]
    [InlineData("/product/starter-smoke-product", "Product detail", "Starter Smoke Product")]
    [InlineData("/category/starter-category", "Starter Category", "Starter Smoke Product")]
    [InlineData("/search?q=starter", "Search", "Results for starter")]
    [InlineData("/my-cart", "Cart", "Cart shell")]
    [InlineData("/checkout", "Checkout", "Cart is empty")]
    [InlineData("/account", "Account", "Account host")]
    public async Task StarterRoute_RendersPresentationContextShell(string path, string expectedTitle, string expectedBody)
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedTitle, content, StringComparison.Ordinal);
        Assert.Contains(expectedBody, content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StarterRoot_RendersPresentationOwnedSecurityHeadAndCoreScripts()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("name=\"blazorshop-antiforgery-token\"", content, StringComparison.Ordinal);
        Assert.Contains("src=\"_framework/blazor.web.js\"", content, StringComparison.Ordinal);
        Assert.Contains("src=\"_content/BlazorShop.Storefront.Presentation/js/storefront.application.js\"", content, StringComparison.Ordinal);
        Assert.Contains("href=\"css/starter.css\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StarterRobotsAndSitemap_UsePresentationEndpoints()
    {
        using var client = CreateClient();

        using var robots = await client.GetAsync(StorefrontRoutes.Robots);
        using var sitemap = await client.GetAsync(StorefrontRoutes.Sitemap);
        var robotsContent = await robots.Content.ReadAsStringAsync();
        var sitemapContent = await sitemap.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, robots.StatusCode);
        Assert.Equal("text/plain", robots.Content.Headers.ContentType?.MediaType);
        Assert.Contains("User-agent: *", robotsContent, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, sitemap.StatusCode);
        Assert.Equal("application/xml", sitemap.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<urlset", sitemapContent, StringComparison.Ordinal);
    }

    private HttpClient CreateClient()
    {
        var configuredFactory = this.factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStorefrontCurrentStoreProvider>();
                services.RemoveAll<IStorefrontDisplayContextProvider>();
                services.RemoveAll<IStorefrontCatalogClient>();
                services.RemoveAll<IStorefrontContentClient>();
                services.RemoveAll<IStorefrontStoreConfigurationClient>();
                services.RemoveAll<IStorefrontCartClient>();
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<IStorefrontRuntimeCheckoutFacade>();
                services.RemoveAll<IStorefrontRuntimePaymentFacade>();
                services.RemoveAll<IStorefrontRuntimeAddressFacade>();
                services.RemoveAll<IStorefrontRuntimeCatalogFacade>();
                services.RemoveAll<IStorefrontRobotsService>();
                services.RemoveAll<IStorefrontSitemapService>();

                services.AddScoped<IStorefrontCurrentStoreProvider>(_ =>
                    new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.Succeeded(CreateCurrentStore())));
                services.AddScoped<IStorefrontDisplayContextProvider>(_ => new StubDisplayContextProvider());
                services.AddScoped<IStorefrontCatalogClient>(_ => new StubCatalogClient());
                services.AddScoped<IStorefrontContentClient>(_ => new StubContentClient());
                services.AddScoped<IStorefrontStoreConfigurationClient>(_ => new StubStoreConfigurationClient());
                services.AddScoped<IStorefrontCartClient>(_ => new StubCartClient());
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubSessionResolver());
                services.AddScoped<IStorefrontRuntimeCheckoutFacade>(_ => new StubRuntimeCheckoutFacade());
                services.AddScoped<IStorefrontRuntimePaymentFacade>(_ => new StubRuntimePaymentFacade());
                services.AddScoped<IStorefrontRuntimeAddressFacade>(_ => new StubRuntimeAddressFacade());
                services.AddScoped<IStorefrontRuntimeCatalogFacade>(_ => new StubRuntimeCatalogFacade());
                services.AddScoped<IStorefrontRobotsService>(_ => new StubRobotsService());
                services.AddScoped<IStorefrontSitemapService>(_ => new StubSitemapService());
            });
        });

        return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    private static StorefrontCurrentStore CreateCurrentStore()
    {
        return new StorefrontCurrentStore(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "starter",
            "Starter Fixture Store",
            "active",
            "https://starter.example.test",
            "starter.example.test",
            true,
            null,
            null,
            "Starter Fixture Co",
            "company@starter.example.test",
            "5550100",
            "1 Starter Street",
            null,
            null,
            null,
            null,
            null,
            "USD",
            "en-US",
            "support@starter.example.test",
            "5550101",
            false,
            null,
            null);
    }

    private static StorefrontDisplayContext CreateDisplayContext()
    {
        return StorefrontDisplayContext.Fallback with
        {
            StoreKey = "starter",
            StoreName = "Starter Fixture Store",
            CurrencyCode = "USD",
            DefaultCurrencyCode = "USD",
            SupportedCurrencyCodes = ["USD"],
        };
    }

    private static StorefrontPublicConfiguration CreatePublicConfiguration()
    {
        return new StorefrontPublicConfiguration(
            new StorefrontStoreIdentity(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "starter",
                "Starter Fixture Store",
                "active",
                "https://starter.example.test",
                "starter.example.test",
                true),
            new StorefrontBranding(null, null, "Starter Fixture Co", null, null, null, null, null, null, null, null, null, null, null),
            new StorefrontLocaleOptions("en-US", ["en-US"]),
            new StorefrontCurrencyOptions("USD", ["USD"]),
            new StorefrontConsentConfiguration(false, false, "v1", "/privacy", [], 365),
            new StorefrontCaptchaConfiguration(false, "none", null, [], new Dictionary<string, string>(StringComparer.Ordinal)),
            new StorefrontMaintenanceState(false, null),
            new StorefrontFeatureFlags(true, true, true, true, true, true),
            new Dictionary<string, StorefrontCapability>(StringComparer.Ordinal)
            {
                ["recommendations"] = new(true, true, null),
            },
            [],
            new StorefrontSeoDefaults("Starter Fixture Store", null, "Starter fixture storefront.", null, "https://starter.example.test", null, null, null, null, null, null, null, null));
    }

    private static GetCatalogProduct CreateCatalogProduct()
    {
        return new GetCatalogProduct
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Slug = "starter-smoke-product",
            Name = "Starter Smoke Product",
            ShortDescription = "Fixture product for Starter host smoke tests.",
            Price = 12.34m,
            DisplayPrice = 12.34m,
            DisplayCurrencyCode = "USD",
            Image = "/media/products/starter-smoke-product.webp",
            CreatedOn = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            InStock = true,
            Quantity = 10,
            Purchasable = true,
            StockStatus = "in_stock",
            CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            CategoryName = "Starter Category",
            CategorySlug = "starter-category",
        };
    }

    private static GetProduct CreateProduct()
    {
        return new GetProduct
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Slug = "starter-smoke-product",
            Name = "Starter Smoke Product",
            ShortDescription = "Fixture product for Starter host smoke tests.",
            Description = "Fixture product for Starter host smoke tests.",
            Price = 12.34m,
            DisplayPrice = 12.34m,
            DisplayCurrencyCode = "USD",
            Image = "/media/products/starter-smoke-product.webp",
            CreatedOn = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            InStock = true,
            Quantity = 10,
            AvailableQuantity = 10,
            Purchasable = true,
            StockStatus = "in_stock",
            Category = CreateCategory(),
        };
    }

    private static GetCategory CreateCategory()
    {
        return new GetCategory
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Slug = "starter-category",
            Name = "Starter Category",
            Description = "Fixture category for Starter host smoke tests.",
        };
    }

    private static PagedResult<GetCatalogProduct> CreateCatalogPage()
    {
        return new PagedResult<GetCatalogProduct>
        {
            Items = [CreateCatalogProduct()],
            PageNumber = 1,
            PageSize = 24,
            TotalCount = 1,
        };
    }

    private static StorefrontRuntimeError Unavailable()
    {
        return new StorefrontRuntimeError(
            StorefrontRuntimeStatusCodes.ServiceUnavailable,
            "fixture.unavailable",
            "Fixture service is unavailable.",
            null,
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));
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

    private sealed class StubDisplayContextProvider : IStorefrontDisplayContextProvider
    {
        public Task<StorefrontDisplayContext> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDisplayContext());
        }
    }

    private sealed class StubCatalogClient : IStorefrontCatalogClient
    {
        public Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<IReadOnlyList<GetCategory>>.Success([CreateCategory()]));
        }

        public Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<GetCategoryTreeNode> tree =
            [
                new GetCategoryTreeNode
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Starter Category",
                    Slug = "starter-category",
                    IsPublished = true,
                },
            ];
            return Task.FromResult(StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.Success(tree));
        }

        public Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<GetPublicCatalogSitemap>.Success(new GetPublicCatalogSitemap()));
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<PagedResult<GetCatalogProduct>>.Success(CreateCatalogPage()));
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, string? currencyCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<PagedResult<GetCatalogProduct>>.Success(CreateCatalogPage()));
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return this.GetPublishedCategoryBySlugAsync(slug, null, cancellationToken);
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, string? currencyCode, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(slug, "starter-category", StringComparison.Ordinal))
            {
                return Task.FromResult(StorefrontApiResult<GetCategoryPage>.NotFound());
            }

            return Task.FromResult(StorefrontApiResult<GetCategoryPage>.Success(new GetCategoryPage
            {
                Category = CreateCategory(),
                Breadcrumbs = [new GetCategoryBreadcrumbItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Starter Category", Slug = "starter-category" }],
                Products = [CreateCatalogProduct()],
                DirectProductCount = 1,
                DescendantProductCount = 1,
            }));
        }

        public Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(string? categorySlug = null, string? searchTerm = null, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<StorefrontProductFilterMetadataResponse>.Success(new StorefrontProductFilterMetadataResponse([], [], [], new StorefrontPriceFacetResponse(null, null, "USD", 0), 2)));
        }

        public Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(string? searchTerm, string? categorySlug = null, int? limit = null, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<StorefrontSearchSuggestionResponse>.Success(new StorefrontSearchSuggestionResponse(searchTerm, 2, limit ?? 6, [])));
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return this.GetPublishedProductBySlugAsync(slug, null, cancellationToken);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, string? currencyCode, CancellationToken cancellationToken = default)
        {
            return string.Equals(slug, "starter-smoke-product", StringComparison.Ordinal)
                ? Task.FromResult(StorefrontApiResult<GetProduct>.Success(CreateProduct()))
                : Task.FromResult(StorefrontApiResult<GetProduct>.NotFound());
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return this.GetProductByIdAsync(id, null, cancellationToken);
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, string? currencyCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<GetProduct>.Success(CreateProduct()));
        }

        public Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(Guid productId, StorefrontProductSelectionPreviewRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed("Preview is not used by the Starter smoke fixture."));
        }
    }

    private sealed class StubContentClient : IStorefrontContentClient
    {
        public Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<GetStorefrontPage>.Success(new GetStorefrontPage
            {
                Slug = slug,
                Title = "Starter Fixture Store",
                BodyHtml = "<p>Starter fixture content.</p>",
                UpdatedAt = DateTimeOffset.UtcNow,
            }));
        }

        public Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.Success([]));
        }

        public Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(string systemName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<StoreNavigationPublicMenuDto>.Success(new StoreNavigationPublicMenuDto(systemName, DateTimeOffset.UtcNow, [])));
        }

        public Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<GetSeoSettings>.Success(new GetSeoSettings
            {
                SiteName = "Starter Fixture Store",
                BaseCanonicalUrl = "https://starter.example.test",
            }));
        }

        public Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<SeoRedirectResolutionDto>.NotFound());
        }
    }

    private sealed class StubStoreConfigurationClient : IStorefrontStoreConfigurationClient
    {
        public Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<StorefrontCurrentStore>.Success(CreateCurrentStore()));
        }

        public Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontApiResult<StorefrontPublicConfiguration>.Success(CreatePublicConfiguration()));
        }

        public Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(StorefrontCurrencyPreferenceRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Succeeded(new StorefrontCurrencyPreferenceResponse("USD", "USD", request.CurrencyCode, true, true, "supported"), "Currency set."));
        }
    }

    private sealed class StubCartClient : IStorefrontCartClient
    {
        public Task<StorefrontSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeCartSessionAsync(string? cartToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartSessionResponse>.Succeeded(new StorefrontCartSessionResponse(Guid.NewGuid(), "starter-cart", "active", 1, DateTimeOffset.UtcNow.AddDays(30)), "Cart session created."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> GetCartAsync(string cartToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Succeeded(new StorefrontCartResponse(Guid.NewGuid(), "active", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), []), "Cart loaded."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> AddCartLineAsync(string cartToken, StorefrontCartLineCreateRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Failed("Not used by smoke tests."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> UpdateCartLineAsync(string cartToken, Guid lineId, StorefrontCartLineUpdateRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Failed("Not used by smoke tests."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> RemoveCartLineAsync(string cartToken, Guid lineId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Failed("Not used by smoke tests."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> ClearCartAsync(string cartToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Failed("Not used by smoke tests."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> RecalculateCartAsync(string cartToken, StorefrontCartRecalculateRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Failed("Not used by smoke tests."));
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> MergeCurrentCustomerCartAsync(string cartToken, string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCartResponse>.Failed("Not used by smoke tests."));
        }
    }

    private sealed class StubSessionResolver : IStorefrontSessionResolver
    {
        public Task<StorefrontSessionInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorefrontSessionInfo(true, false, "Starter Customer", "customer@starter.example.test", "access-token"));
        }
    }

    private sealed class StubRuntimeCheckoutFacade : IStorefrontRuntimeCheckoutFacade
    {
        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutPreviewResponse>> PreviewAsync(string? cartToken, RuntimeCheckoutPreviewRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutPreviewResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>> StartAsync(string? cartToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>> LoadAsync(string? cartToken, Guid checkoutSessionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>> UpdateAddressesAsync(string? cartToken, Guid checkoutSessionId, RuntimeCheckoutAddressStepRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>> UpdateAddressesAsync(string? cartToken, Guid checkoutSessionId, RuntimeCheckoutAddressStepRequest request, string? bearerToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>> SelectShippingMethodAsync(string? cartToken, Guid checkoutSessionId, RuntimeCheckoutShippingMethodRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>> SelectPaymentMethodAsync(string? cartToken, Guid checkoutSessionId, RuntimeCheckoutPaymentMethodRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutSessionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeCheckoutReviewResponse>> ReviewAsync(string? cartToken, Guid checkoutSessionId, RuntimeCheckoutReviewRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeCheckoutReviewResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimePlaceOrderResponse>> PlaceOrderAsync(RuntimePlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimePlaceOrderResponse>.Failed(Unavailable()));
        }
    }

    private sealed class StubRuntimePaymentFacade : IStorefrontRuntimePaymentFacade
    {
        public Task<StorefrontRuntimeResult<IReadOnlyList<RuntimePaymentMethodResponse>>> ListMethodsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<RuntimePaymentMethodResponse>>.Succeeded([]));
        }

        public Task<StorefrontRuntimeResult<RuntimePaymentAttemptResponse>> GetAttemptAsync(Guid paymentAttemptId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimePaymentAttemptResponse>.Failed(Unavailable()));
        }
    }

    private sealed class StubRuntimeAddressFacade : IStorefrontRuntimeAddressFacade
    {
        public Task<StorefrontRuntimeResult<IReadOnlyList<RuntimeAddressCountryResponse>>> ListCountriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<RuntimeAddressCountryResponse>>.Succeeded([]));
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<RuntimeAddressStateProvinceResponse>>> ListStatesAsync(string? countryCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<RuntimeAddressStateProvinceResponse>>.Succeeded([]));
        }

        public Task<StorefrontRuntimeResult<RuntimeAddressConfigurationResponse>> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeAddressConfigurationResponse>.Succeeded(null!));
        }
    }

    private sealed class StubRuntimeCatalogFacade : IStorefrontRuntimeCatalogFacade
    {
        public Task<StorefrontRuntimeResult<IReadOnlyList<RuntimeCategoryResponse>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<RuntimeCategoryResponse>>.Succeeded([]));
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<RuntimeCategoryTreeNodeResponse>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<RuntimeCategoryTreeNodeResponse>>.Succeeded([]));
        }

        public Task<StorefrontRuntimeResult<RuntimePublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimePublicCatalogSitemap>.Succeeded(new RuntimePublicCatalogSitemap()));
        }

        public Task<StorefrontRuntimeResult<RuntimeCatalogPagedResponse>> GetPublishedCatalogPageAsync(StorefrontRuntimeProductCatalogQuery query, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeCatalogPagedResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeResult<RuntimeCategoryPageResponse>> GetPublishedCategoryBySlugAsync(string slug, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeCategoryPageResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeResult<RuntimeProductFilterMetadataResponse>> GetProductFilterMetadataAsync(string? categorySlug = null, string? searchTerm = null, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeProductFilterMetadataResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeResult<RuntimeSearchSuggestionResponse>> GetSearchSuggestionsAsync(string? searchTerm, string? categorySlug = null, int? limit = null, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeSearchSuggestionResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeResult<RuntimeProductResponse>> GetPublishedProductBySlugAsync(string slug, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeProductResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeResult<RuntimeProductResponse>> GetProductByIdAsync(Guid id, string? currencyCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeResult<RuntimeProductResponse>.Failed(Unavailable()));
        }

        public Task<StorefrontRuntimeSubmitResult<RuntimeProductSelectionPreviewResponse>> PreviewProductSelectionAsync(Guid productId, RuntimeProductSelectionPreviewRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontRuntimeSubmitResult<RuntimeProductSelectionPreviewResponse>.Failed(Unavailable()));
        }
    }

    private sealed class StubRobotsService : IStorefrontRobotsService
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("User-agent: *\nAllow: /\n");
        }
    }

    private sealed class StubSitemapService : IStorefrontSitemapService
    {
        public Task<StorefrontSitemapGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StorefrontSitemapGenerationResult.Success("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" />"));
        }
    }
}
