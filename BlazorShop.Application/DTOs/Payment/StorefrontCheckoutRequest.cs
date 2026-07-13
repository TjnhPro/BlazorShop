namespace BlazorShop.Application.DTOs.Payment
{
    public sealed class StorefrontCheckoutRequest
    {
        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string PaymentMethodKey { get; set; } = string.Empty;

        public required IEnumerable<ProcessCart> Carts { get; set; }

        public CheckoutShippingAddress ShippingAddress { get; set; } = new();
    }

    public sealed class CheckoutShippingAddress
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

    public sealed record StorefrontCheckoutResult(
        Guid OrderId,
        string Reference,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime CreatedOn);
}
