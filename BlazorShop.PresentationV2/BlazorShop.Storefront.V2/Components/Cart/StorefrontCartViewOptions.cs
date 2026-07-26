namespace BlazorShop.Storefront.Components.Cart;

using BlazorShop.Storefront.Components.Headless.Cart;

public static class StorefrontCartViewOptions
{
    public static StorefrontCartActionDescriptor Actions { get; } = new(
        "/api/cart",
        "/api/cart/lines/{lineId}",
        "/api/cart/lines/{lineId}",
        "/api/cart");

    public static StorefrontCartViewClasses Classes { get; } = new()
    {
        PageSection = "mx-auto max-w-7xl px-4 pb-12 pt-10 sm:px-6 lg:px-8",
        Layout = "flex flex-col gap-6 lg:flex-row lg:items-start",
        ContentColumn = "flex-1 space-y-4",
        HeaderCard = "rounded-3xl border border-neutral-200/70 bg-white/90 p-8 shadow-lg",
        HeaderLayout = "flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between",
        Eyebrow = "text-sm font-semibold uppercase tracking-[0.2em] text-neutral-500",
        PageTitle = "mt-2 text-4xl font-extrabold tracking-tight text-neutral-900",
        BodyText = "mt-3 max-w-2xl text-base leading-7 text-neutral-700",
        CountBadge = "inline-flex items-center rounded-full bg-blue-50 px-4 py-2 text-sm font-semibold text-blue-900 shadow-sm",
        Alert = "rounded-2xl border px-4 py-3 text-sm leading-6",
        ErrorAlert = "border-rose-200 bg-rose-50 text-rose-900",
        WarningAlert = "border-amber-200 bg-amber-50 text-amber-900",
        EmptyState = "rounded-3xl border border-dashed border-neutral-300 bg-white/80 px-6 py-10 text-center shadow-sm",
        EmptyTitle = "text-2xl font-bold text-neutral-900",
        EmptyActions = "mt-6 flex flex-wrap justify-center gap-3",
        PrimaryLink = "inline-flex items-center rounded bg-neutral-900 px-4 py-2 font-semibold text-white hover:bg-neutral-800",
        SecondaryLink = "inline-flex items-center rounded bg-amber-500 px-4 py-2 font-semibold text-white hover:bg-amber-600",
        LineList = "space-y-4",
        LineCard = "rounded-3xl border border-neutral-200/70 bg-white/95 p-5 shadow-md",
        LineLayout = "flex flex-col gap-4 sm:flex-row",
        LineImageFrame = "flex h-24 w-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-neutral-50 ring-1 ring-black/5",
        LineImage = "h-full w-full object-contain",
        LineTitle = "text-xl font-bold text-neutral-900 hover:text-neutral-700",
        LineMeta = "mt-1 text-sm font-medium text-neutral-500",
        LineWarning = "mt-3 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm leading-6 text-amber-900",
        LineControls = "mt-4 grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto_auto] lg:items-end",
        LineMetrics = "grid gap-4 sm:grid-cols-3",
        MetricLabel = "text-xs font-semibold uppercase tracking-[0.16em] text-neutral-500",
        MetricValue = "mt-1 text-lg font-bold text-neutral-900",
        QuantityInput = "mt-1 w-24 rounded-xl border border-neutral-300 bg-white px-3 py-2 text-sm font-semibold text-neutral-900 focus:border-neutral-500 focus:outline-none disabled:cursor-wait disabled:bg-neutral-100",
        RemoveButton = "inline-flex items-center justify-center rounded bg-white px-4 py-2 text-sm font-semibold text-rose-700 ring-1 ring-rose-200 hover:bg-rose-50 disabled:cursor-wait disabled:bg-neutral-100",
        SummaryAside = "w-full lg:sticky lg:top-6 lg:max-w-sm",
        SummaryCard = "rounded-3xl border border-neutral-200/70 bg-white/95 p-6 shadow-lg",
        SummaryRows = "mt-6 space-y-3 text-sm text-neutral-700",
        SummaryRow = "flex items-center justify-between gap-3",
        CheckoutButton = "inline-flex w-full items-center justify-center rounded bg-amber-500 px-4 py-3 font-semibold text-white hover:bg-amber-600",
        DisabledCheckoutButton = "inline-flex w-full cursor-not-allowed items-center justify-center rounded bg-neutral-300 px-4 py-3 font-semibold text-neutral-600",
        ClearButton = "inline-flex w-full items-center justify-center rounded bg-white px-4 py-3 font-semibold text-neutral-900 ring-1 ring-black/10 hover:bg-neutral-100 disabled:cursor-wait disabled:bg-neutral-100"
    };
}
