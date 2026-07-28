namespace BlazorShop.Storefront.Presentation.Services.Checkout
{
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Presentation.Contracts;

    public sealed record StorefrontCheckoutPageContext(
        string? Error,
        string? OrderReference,
        StorefrontBrowserCheckoutState CheckoutState,
        IReadOnlyList<StorefrontCheckoutPageLine> Lines,
        IReadOnlyList<StorefrontCheckoutPaymentMethodOptionView> PaymentMethods,
        IReadOnlyList<StorefrontCheckoutAddressCountryView> AddressCountries,
        IReadOnlyList<StorefrontCheckoutAddressStateProvinceView> AddressStates,
        StorefrontCheckoutAddressFieldConfigurationView? AddressConfiguration,
        int CartVersion,
        string IdempotencyKey,
        string GrandTotalDisplay,
        string GrandTotalCurrencyCode,
        decimal? ServerSubtotal,
        decimal? ServerShippingTotal,
        decimal? ServerTaxTotal,
        decimal? ServerDiscountTotal,
        string? ServerSubtotalDisplay,
        string? ServerShippingTotalDisplay,
        string? ServerTaxTotalDisplay,
        string? ServerDiscountTotalDisplay,
        string DefaultShippingCountryCode,
        string DefaultShippingStateCode,
        StorefrontLinkContext Links)
    {
        public bool HasAddressCountries => AddressCountries.Count > 0;

        public bool HasAddressStates => AddressStates.Count > 0;

        public bool PhoneEnabled => AddressConfiguration?.PhoneEnabled ?? true;

        public bool PhoneRequired => AddressConfiguration?.PhoneRequired ?? false;

        public bool PostalCodeRequired => AddressConfiguration?.PostalCodeRequired ?? true;

        public bool HasOrderReference => !string.IsNullOrWhiteSpace(OrderReference);
    }

    public sealed record StorefrontCheckoutPageLine(
        string DisplayName,
        int Quantity,
        decimal UnitPrice,
        string CurrencyCode,
        string LineTotalDisplay)
    {
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public sealed record StorefrontCheckoutPaymentMethodOptionView(
        string Key,
        string DisplayName,
        string? Description);

    public sealed record StorefrontCheckoutAddressCountryView(
        string Code,
        string Name);

    public sealed record StorefrontCheckoutAddressStateProvinceView(
        string Code,
        string Name);

    public sealed record StorefrontCheckoutAddressFieldConfigurationView(
        bool PhoneEnabled,
        bool PhoneRequired,
        bool PostalCodeRequired);
}
