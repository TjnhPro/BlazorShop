namespace BlazorShop.Storefront.Services
{
    public sealed class StorefrontCheckoutForm
    {
        public int CartVersion { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerName { get; set; }

        public string? PaymentMethodKey { get; set; }

        public string? ShippingFullName { get; set; }

        public string? ShippingEmail { get; set; }

        public string? ShippingPhone { get; set; }

        public string? ShippingAddress1 { get; set; }

        public string? ShippingAddress2 { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingState { get; set; }

        public string? ShippingPostalCode { get; set; }

        public string? ShippingCountryCode { get; set; }
    }
}
