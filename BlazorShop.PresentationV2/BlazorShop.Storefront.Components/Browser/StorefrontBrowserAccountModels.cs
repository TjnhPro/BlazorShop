namespace BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontBrowserCustomerProfile(
    Guid CustomerPublicId,
    string Email,
    string FullName,
    string? FirstName,
    string? LastName,
    string? Company,
    string? PhoneNumber,
    string? PreferredLanguage,
    string? PreferredCurrencyCode,
    string CreatedAtDisplay,
    string? LastActivityDisplay);

public sealed class StorefrontBrowserCustomerProfileUpdateRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Company { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PreferredLanguage { get; set; }

    public string? PreferredCurrencyCode { get; set; }
}

public sealed class StorefrontBrowserCustomerAddressRequest
{
    public string FullName { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Address1 { get; set; } = string.Empty;

    public string? Address2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string? StateProvinceCode { get; set; }

    public string? StateProvinceName { get; set; }

    public bool IsDefaultShipping { get; set; }

    public bool IsDefaultBilling { get; set; }
}

public sealed record StorefrontBrowserCustomerAddress(
    Guid PublicId,
    string FullName,
    string? Company,
    string? Email,
    string? Phone,
    string Address1,
    string? Address2,
    string City,
    string PostalCode,
    string CountryCode,
    string? StateProvinceCode,
    string? StateProvinceName,
    bool IsDefaultShipping,
    bool IsDefaultBilling);

public sealed record StorefrontBrowserAccountOrderList(
    IReadOnlyList<StorefrontBrowserAccountOrderListItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record StorefrontBrowserAccountOrderListItem(
    string Reference,
    string CreatedOnDisplay,
    string OrderStatus,
    string PaymentStatus,
    string ShippingStatus,
    string TotalDisplay,
    int ItemCount);

public sealed record StorefrontBrowserAccountOrderDetail(
    string Reference,
    bool ReceiptMode,
    string CreatedOnDisplay,
    string OrderStatus,
    string PaymentStatus,
    string ShippingStatus,
    string TotalDisplay,
    StorefrontBrowserOrderAddress ShippingAddress,
    StorefrontBrowserOrderAddress? BillingAddress,
    IReadOnlyList<StorefrontBrowserAccountOrderLine> Lines,
    StorefrontBrowserOrderTotals Totals);

public sealed record StorefrontBrowserOrderAddress(
    string? FullName,
    string? Email,
    string? Phone,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? PostalCode,
    string? CountryCode);

public sealed record StorefrontBrowserAccountOrderLine(
    string? ProductName,
    string? Sku,
    int Quantity,
    string LineTotalDisplay);

public sealed record StorefrontBrowserOrderTotals(
    string SubtotalDisplay,
    string ShippingDisplay,
    string TaxDisplay,
    string DiscountDisplay,
    string GrandTotalDisplay);

public sealed record StorefrontBrowserAccountCommandResult(bool Success, string Message);
