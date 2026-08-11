namespace BlazorShop.Storefront.Components.Contracts.Account;

public sealed record StorefrontAccountProfileLabels
{
    public static StorefrontAccountProfileLabels Empty { get; } = new();

    public string MissingProfile { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string SaveProfile { get; init; } = string.Empty;
    public string Saving { get; init; } = string.Empty;
    public string SavedSuccess { get; init; } = string.Empty;
}

public sealed record StorefrontAccountPasswordLabels
{
    public static StorefrontAccountPasswordLabels Empty { get; } = new();

    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
    public string ChangePassword { get; init; } = string.Empty;
    public string Changing { get; init; } = string.Empty;
    public string SavedSuccess { get; init; } = string.Empty;
}

public sealed record StorefrontAccountAddressBookLabels
{
    public static StorefrontAccountAddressBookLabels Empty { get; } = new();

    public string AddAddress { get; init; } = string.Empty;
    public string SaveAddress { get; init; } = string.Empty;
    public string NoSavedAddresses { get; init; } = string.Empty;
    public string DefaultShipping { get; init; } = string.Empty;
    public string DefaultBilling { get; init; } = string.Empty;
    public string Update { get; init; } = string.Empty;
    public string Delete { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string AddressLine2 { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string StateProvince { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string SavedSuccess { get; init; } = string.Empty;
}

public sealed record StorefrontAccountOrderListLabels
{
    public static StorefrontAccountOrderListLabels Empty { get; } = new();

    public string NoOrders { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Payment { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public string Items { get; init; } = string.Empty;
}

public sealed record StorefrontAccountOrderDetailLabels
{
    public static StorefrontAccountOrderDetailLabels Empty { get; } = new();

    public string OrderStatus { get; init; } = string.Empty;
    public string Payment { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
    public string BillingAddress { get; init; } = string.Empty;
    public string Items { get; init; } = string.Empty;
    public string QuantityPrefix { get; init; } = string.Empty;
    public string Totals { get; init; } = string.Empty;
    public string Subtotal { get; init; } = string.Empty;
    public string Shipping { get; init; } = string.Empty;
    public string Tax { get; init; } = string.Empty;
    public string Discount { get; init; } = string.Empty;
    public string GrandTotal { get; init; } = string.Empty;
}
