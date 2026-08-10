namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Components.Primitives.Product;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontProductGalleryPrimitiveTests
{
    [Fact]
    public async Task NoImagesRenderHostSuppliedFallback()
    {
        var html = await RenderAsync([]);

        Assert.Contains("class=\"gallery-root\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"gallery-main\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-product-gallery", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-main", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-placeholder", html, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Unavailable for Sail Jacket\"", html, StringComparison.Ordinal);
        Assert.Contains("No image supplied", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OneImageRendersMainImageWithoutControlsOrThumbnails()
    {
        var html = await RenderAsync([Image("front")]);

        Assert.Contains("src=\"/media/front.webp\"", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"Front image\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"main-image\"", html, StringComparison.Ordinal);
        Assert.Contains("loading=\"eager\"", html, StringComparison.Ordinal);
        Assert.Contains("onerror=\"this.onerror=null;", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-main-image", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-gallery-controls", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-gallery-thumbnail", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MultipleImagesRenderControlsThumbnailsAndInitialSelection()
    {
        var html = await RenderAsync([Image("front"), Image("side")]);

        Assert.Contains("class=\"gallery-controls\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"previous-button\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"next-button\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Previous image\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Next image\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-prev", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-next", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-thumb-viewport", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Product images\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-thumbnail", html, StringComparison.Ordinal);
        Assert.Contains("data-gallery-index=\"0\"", html, StringComparison.Ordinal);
        Assert.Contains("data-gallery-index=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-selected=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-image-url=\"/media/side.webp\"", html, StringComparison.Ordinal);
        Assert.Contains("data-alt=\"Side image\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"thumb-fallback\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-gallery-thumb-fallback", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimitiveSourceUsesHostClassesAndKeepsApprovedBrokenImageFallback()
    {
        var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Product/StorefrontProductGallery.razor");

        Assert.Contains("BrokenImageFallbackScript", source, StringComparison.Ordinal);
        Assert.Contains("data:image/svg+xml", source, StringComparison.Ordinal);
        Assert.Contains("class=\"@Classes.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("aspect-square", source, StringComparison.Ordinal);
        Assert.DoesNotContain("bs-product-gallery__", source, StringComparison.Ordinal);
        Assert.DoesNotContain("rounded-", source, StringComparison.Ordinal);
    }

    private static ProductGalleryItem Image(string name)
    {
        return new ProductGalleryItem($"/media/{name}.webp", $"/media/{name}-thumb.webp", $"{char.ToUpperInvariant(name[0])}{name[1..]} image");
    }

    private static async Task<string> RenderAsync(IReadOnlyList<ProductGalleryItem> items)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontProductGallery.Items)] = items,
                [nameof(StorefrontProductGallery.ProductName)] = "Sail Jacket",
                [nameof(StorefrontProductGallery.Labels)] = Labels,
                [nameof(StorefrontProductGallery.Classes)] = Classes,
            });

            var component = await renderer.RenderComponentAsync<StorefrontProductGallery>(parameters);
            return component.ToHtmlString();
        });
    }

    private static ProductGalleryLabels Labels { get; } = new(
        "No image supplied",
        "Unavailable for {0}",
        "Previous image",
        "Next image",
        "Product images",
        "Show image {0}");

    private static ProductGalleryClasses Classes { get; } = new(
        Root: "gallery-root",
        Main: "gallery-main",
        MainImage: "main-image",
        Placeholder: "placeholder",
        Controls: "gallery-controls",
        PreviousButton: "previous-button",
        NextButton: "next-button",
        Icon: "icon",
        ThumbnailViewport: "thumb-viewport",
        Thumbnail: "thumb-button",
        ThumbnailImage: "thumb-image",
        ThumbnailFallback: "thumb-fallback");

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
