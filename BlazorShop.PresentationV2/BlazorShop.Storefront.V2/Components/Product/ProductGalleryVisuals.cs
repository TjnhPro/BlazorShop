namespace BlazorShop.Storefront.V2.Components.Product;

using BlazorShop.Storefront.Components.Contracts.Product;

public static class ProductGalleryVisuals
{
    public static ProductGalleryLabels Labels { get; } = new(
        ImageUnavailableText: "Image unavailable",
        ImageUnavailableAltFormat: "Image unavailable for {0}",
        PreviousImage: "Show previous product image",
        NextImage: "Show next product image",
        ImagesRegion: "Product images",
        ImageButtonFormat: "Show product image {0}");

    public static ProductGalleryClasses Classes { get; } = new(
        Root: "bs-product-gallery grid gap-4",
        Main: "bs-product-gallery__main relative aspect-square overflow-hidden rounded-2xl bg-neutral-50",
        MainImage: "bs-product-gallery__image absolute inset-0 h-full w-full object-contain",
        Placeholder: "bs-product-gallery__placeholder absolute inset-0 flex flex-col items-center justify-center gap-2 bg-neutral-100 px-6 text-center text-sm font-semibold text-neutral-500",
        Controls: "bs-product-gallery__controls",
        PreviousButton: "bs-product-gallery__nav bs-product-gallery__nav--prev",
        NextButton: "bs-product-gallery__nav bs-product-gallery__nav--next",
        Icon: "bs-product-gallery__nav-icon",
        ThumbnailViewport: "bs-product-gallery__thumbs flex gap-3 overflow-x-auto pb-1",
        Thumbnail: "bs-product-gallery__thumb relative h-20 w-20 shrink-0 overflow-hidden rounded-xl border border-neutral-200 bg-neutral-50 p-0 ring-0 transition hover:border-neutral-500 focus:outline-none focus:ring-2 focus:ring-neutral-900 data-[selected=true]:border-neutral-900 data-[selected=true]:ring-2 data-[selected=true]:ring-neutral-900",
        ThumbnailImage: "bs-product-gallery__image absolute inset-0 h-full w-full object-contain",
        ThumbnailFallback: "bs-product-gallery__thumb-fallback absolute inset-0 flex items-center justify-center bg-neutral-100 px-2 text-center text-[11px] font-semibold text-neutral-500");
}
