namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductGalleryItem(
    string ImageUrl,
    string ThumbnailUrl,
    string AltText);
