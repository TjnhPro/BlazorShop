namespace BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontBrowserCheckoutState(
    bool HasCart,
    string? Message,
    Guid? CheckoutSessionId,
    int CheckoutVersion,
    int CartVersion,
    string State,
    string CurrentStep,
    bool IsActive,
    bool ShippingRequired,
    bool PlaceOrderAllowed,
    string GrandTotalDisplay,
    IReadOnlyList<StorefrontBrowserCheckoutLine> Lines,
    IReadOnlyList<StorefrontBrowserCheckoutOption> ShippingOptions,
    IReadOnlyList<StorefrontBrowserCheckoutOption> PaymentMethods,
    IReadOnlyList<StorefrontBrowserCheckoutIssue> Issues);

public sealed record StorefrontBrowserCheckoutLine(
    Guid LineId,
    Guid ProductId,
    Guid? ProductVariantId,
    int Quantity,
    string UnitPriceDisplay,
    string LineTotalDisplay);

public sealed record StorefrontBrowserCheckoutOption(
    string Key,
    string DisplayName,
    string? Description,
    string? AmountDisplay,
    bool Selected);

public sealed record StorefrontBrowserCheckoutIssue(
    string Code,
    string Message,
    string? Field);

public sealed class StorefrontBrowserCheckoutAddress
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Address1 { get; set; } = string.Empty;

    public string? Address2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string? State { get; set; }

    public string PostalCode { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;
}

public sealed class StorefrontBrowserCheckoutAddressRequest
{
    public Guid CheckoutSessionId { get; set; }

    public int ExpectedCartVersion { get; set; }

    public Guid? ShippingAddressId { get; set; }

    public Guid? BillingAddressId { get; set; }

    public bool UseShippingAddressAsBillingAddress { get; set; } = true;

    public StorefrontBrowserCheckoutAddress? ShippingAddress { get; set; }

    public StorefrontBrowserCheckoutAddress? BillingAddress { get; set; }
}

public sealed class StorefrontBrowserCheckoutSelectionRequest
{
    public Guid CheckoutSessionId { get; set; }

    public int ExpectedCartVersion { get; set; }

    public string Key { get; set; } = string.Empty;
}

public sealed class StorefrontBrowserCheckoutReviewRequest
{
    public Guid CheckoutSessionId { get; set; }

    public int ExpectedCartVersion { get; set; }

    public bool TermsAccepted { get; set; }

    public string? TermsVersion { get; set; }
}

public sealed class StorefrontBrowserCheckoutPlaceOrderRequest
{
    public Guid CheckoutSessionId { get; set; }

    public int ExpectedCheckoutVersion { get; set; }

    public int ExpectedCartVersion { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed record StorefrontBrowserCheckoutPlaceOrderResult(
    bool Success,
    string Message,
    string? OrderReference,
    string? RedirectUrl);
