namespace BlazorShop.Storefront.Components.Headless.Product;

using BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductGalleryState(
    IReadOnlyList<ProductGalleryItem> Items,
    int SelectedIndex,
    string DisplayProductName)
{
    public const string GalleryHook = "data-storefront-product-gallery";
    public const string MainHook = "data-storefront-gallery-main";
    public const string MainImageHook = "data-storefront-gallery-main-image";
    public const string PlaceholderHook = "data-storefront-gallery-placeholder";
    public const string ControlsHook = "data-storefront-gallery-controls";
    public const string PreviousHook = "data-storefront-gallery-prev";
    public const string NextHook = "data-storefront-gallery-next";
    public const string ThumbnailViewportHook = "data-storefront-gallery-thumb-viewport";
    public const string ThumbnailHook = "data-storefront-gallery-thumbnail";
    public const string ThumbnailFallbackHook = "data-storefront-gallery-thumb-fallback";

    public ProductGalleryItem? SelectedItem => Items.Count == 0 ? null : Items[SelectedIndex];

    public bool HasItems => Items.Count > 0;

    public bool HasMultipleItems => Items.Count > 1;

    public bool CanSelectPrevious => SelectedIndex > 0;

    public bool CanSelectNext => SelectedIndex < Items.Count - 1;

    public string SelectedAltText => SelectedItem?.AltText ?? FallbackAltText;

    public string FallbackAltText => $"Image unavailable for {DisplayProductName}";

    public ProductGalleryState Select(int index)
    {
        if (Items.Count == 0)
        {
            return this with { SelectedIndex = 0 };
        }

        return this with { SelectedIndex = Math.Clamp(index, 0, Items.Count - 1) };
    }

    public ProductGalleryState SelectPrevious() => Select(SelectedIndex - 1);

    public ProductGalleryState SelectNext() => Select(SelectedIndex + 1);

    public static ProductGalleryState Create(IReadOnlyList<ProductGalleryItem> items, string? productName)
    {
        var normalizedItems = items.Count == 0 ? Array.Empty<ProductGalleryItem>() : items;
        var displayName = string.IsNullOrWhiteSpace(productName) ? "product" : productName.Trim();
        return new ProductGalleryState(normalizedItems, 0, displayName);
    }
}
