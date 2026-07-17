namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class CheckoutSteps
    {
        public const string Entry = "entry";

        public const string BillingAddress = "billing_address";

        public const string ShippingAddress = "shipping_address";

        public const string ShippingMethod = "shipping_method";

        public const string PaymentMethod = "payment_method";

        public const string Review = "review";

        public const string PlaceOrder = "place_order";

        public const string Complete = "complete";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Entry,
            BillingAddress,
            ShippingAddress,
            ShippingMethod,
            PaymentMethod,
            Review,
            PlaceOrder,
            Complete,
        };
    }
}
