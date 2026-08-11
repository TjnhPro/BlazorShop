namespace BlazorShop.Storefront.V2.Components.Layout;

using BlazorShop.Storefront.Components.Contracts.System;

public static class StorefrontToastVisuals
{
    public static StorefrontToastRegionLabels Labels { get; } = new("Dismiss notification");
    public static StorefrontToastRegionClasses Classes { get; } = new(
        Region: "pointer-events-none fixed inset-x-0 top-4 z-[100] flex justify-end px-4 sm:px-6 lg:px-8",
        Toast: "bs-storefront-toast pointer-events-auto w-full max-w-sm overflow-hidden rounded-2xl border border-white/10 text-white shadow-2xl ring-1 ring-black/10",
        Content: "flex items-start gap-3 px-4 py-3",
        Accent: "mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-full",
        Icon: "h-5 w-5",
        Text: "min-w-0 flex-1",
        Heading: "text-sm font-semibold leading-5",
        Message: "mt-1 text-sm leading-5 text-white/85",
        CloseButton: "rounded-full border border-white/10 bg-white/5 p-1 text-white/70 transition hover:bg-white/10 hover:text-white",
        CloseIcon: "h-4 w-4");
}
