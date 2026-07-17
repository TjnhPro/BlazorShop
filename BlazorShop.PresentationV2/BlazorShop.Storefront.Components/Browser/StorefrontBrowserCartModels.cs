namespace BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontBrowserCart(
    int Count,
    int Version,
    IReadOnlyList<StorefrontBrowserCartLine> Lines,
    string CurrencyCode,
    decimal Subtotal,
    string SubtotalDisplay,
    decimal GrandTotal,
    string GrandTotalDisplay,
    bool CheckoutAllowed,
    IReadOnlyList<StorefrontBrowserCartWarning> Warnings,
    IReadOnlyList<StorefrontBrowserCartAdjustment> Adjustments);

public sealed record StorefrontBrowserCartLine(
    Guid LineId,
    Guid ProductId,
    Guid? ProductVariantId,
    string DisplayName,
    string? ProductUrl,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    string UnitPriceDisplay,
    decimal LineTotal,
    string LineTotalDisplay,
    string CurrencyCode,
    string? VariantLabel,
    int QuantityMinimum,
    int? QuantityMaximum,
    int QuantityStep,
    IReadOnlyList<StorefrontBrowserCartWarning> Warnings,
    bool IsUnavailable);

public sealed record StorefrontBrowserCartWarning(string Message);

public sealed record StorefrontBrowserCartAdjustment(string Label, decimal Amount, string AmountDisplay);

public sealed record StorefrontBrowserCartAlert(string Level, string Message);

public sealed record StorefrontBrowserCartQuantityRequest(int Quantity);
