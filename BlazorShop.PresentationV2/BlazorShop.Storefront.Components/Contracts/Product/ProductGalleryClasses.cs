namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductGalleryClasses(
    string Root = "",
    string Main = "",
    string MainImage = "",
    string Placeholder = "",
    string Controls = "",
    string PreviousButton = "",
    string NextButton = "",
    string Icon = "",
    string ThumbnailViewport = "",
    string Thumbnail = "",
    string ThumbnailImage = "",
    string ThumbnailFallback = "");
