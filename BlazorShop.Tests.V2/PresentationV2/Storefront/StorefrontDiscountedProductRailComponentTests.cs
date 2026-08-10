namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Browser.Catalog;
using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Components.WasmHost.Catalog;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontDiscountedProductRailComponentTests
{
    [Fact]
    public async Task RendersSuccessProductsFromBrowserController()
    {
        var response = new StorefrontDiscountedProductRailResponse([Product("discounted-product")]);
        var controller = new RecordingProductRailController(response);

        var html = await RenderAsync(controller);

        Assert.Contains("data-storefront-component=\"discounted-product-rail\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-product-rail-list", html, StringComparison.Ordinal);
        Assert.Contains("Discounted Product", html, StringComparison.Ordinal);
        Assert.Contains("USD 10.00", html, StringComparison.Ordinal);
        Assert.Contains("USD 12.00", html, StringComparison.Ordinal);
        Assert.Equal(6, controller.RequestedLimits.Single());
    }

    [Fact]
    public async Task RendersEmptyStateWhenControllerReturnsNoProducts()
    {
        var controller = new RecordingProductRailController(new StorefrontDiscountedProductRailResponse([]));

        var html = await RenderAsync(controller);

        Assert.Contains("data-storefront-product-rail-empty", html, StringComparison.Ordinal);
        Assert.Contains("No discounted products", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-product-rail-item", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RendersRetryableErrorState()
    {
        var controller = new RecordingProductRailController(new StorefrontDiscountedProductRailResponse(
            [],
            Success: false,
            Code: "service_unavailable",
            DefaultMessage: "Catalog unavailable",
            TraceId: "trace-rail",
            Retryable: true));

        var html = await RenderAsync(controller);

        Assert.Contains("data-storefront-product-rail-error", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-error-code=\"service_unavailable\"", html, StringComparison.Ordinal);
        Assert.Contains("Catalog unavailable", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-product-rail-retry", html, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceUsesBrowserControllerWithoutDirectTransportOrRenderMode()
    {
        var source = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Catalog/StorefrontDiscountedProductRail.razor"));

        Assert.Contains("@inject IStorefrontBrowserProductRailController ProductRailController", source, StringComparison.Ordinal);
        Assert.Contains("ProductRailController.GetDiscountedProductRailAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/api/", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(RecordingProductRailController controller)
    {
        var services = new ServiceCollection()
            .AddSingleton<IStorefrontBrowserProductRailController>(controller)
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontDiscountedProductRail.Labels)] = new StorefrontDiscountedProductRailLabels(
                    "Discounted",
                    "Loading discounted products",
                    "No discounted products",
                    "Discounted products unavailable",
                    "Retry"),
                [nameof(StorefrontDiscountedProductRail.Classes)] = new StorefrontDiscountedProductRailClasses(
                    Root: "rail-root",
                    Header: "rail-header",
                    Heading: "rail-heading",
                    Body: "rail-body",
                    List: "rail-list",
                    Item: "rail-item",
                    Loading: "rail-loading",
                    Empty: "rail-empty",
                    Error: "rail-error",
                    RetryButton: "rail-retry"),
                [nameof(StorefrontDiscountedProductRail.Action)] =
                    new StorefrontDiscountedProductRailActionDescriptor("local/discounted-products"),
                [nameof(StorefrontDiscountedProductRail.Limit)] = 6,
            });

            var component = await renderer.RenderComponentAsync<StorefrontDiscountedProductRail>(parameters);
            return component.ToHtmlString();
        });
    }

    private static ProductSummaryItem Product(string slug)
    {
        return new ProductSummaryItem(
            Guid.NewGuid(),
            slug.Replace('-', ' ').ToTitleCaseInvariant(),
            "/" + slug,
            "Category",
            "/category",
            ImageUrl: null,
            Description: null,
            "USD 10.00",
            "USD 12.00",
            HasVariants: false,
            InStock: true,
            IsNewArrival: false,
            Purchasable: true,
            PurchaseUrl: "/" + slug + "#purchase");
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class RecordingProductRailController : IStorefrontBrowserProductRailController
    {
        private readonly StorefrontDiscountedProductRailResponse response;

        public RecordingProductRailController(StorefrontDiscountedProductRailResponse response)
        {
            this.response = response;
        }

        public List<int> RequestedLimits { get; } = [];

        public Task<StorefrontDiscountedProductRailResponse> GetDiscountedProductRailAsync(
            int limit,
            StorefrontDiscountedProductRailActionDescriptor? actionDescriptor = null,
            CancellationToken cancellationToken = default)
        {
            this.RequestedLimits.Add(limit);
            return Task.FromResult(this.response);
        }
    }
}
