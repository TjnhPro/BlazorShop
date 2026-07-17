namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class TransactionalMessageTemplateSystemNames
    {
        public const string AccountActivation = "customer.account_activation";
        public const string PasswordRecovery = "customer.password_recovery";
        public const string OrderPlaced = "order.placed";
        public const string OrderPaymentStatusChanged = "order.payment_status_changed";
        public const string OrderFulfillmentStatusChanged = "order.fulfillment_status_changed";
        public const string StorefrontContactForm = "storefront.contact_form";

        public static readonly IReadOnlySet<string> Required = new HashSet<string>(StringComparer.Ordinal)
        {
            AccountActivation,
            PasswordRecovery,
            OrderPlaced,
            OrderPaymentStatusChanged,
            OrderFulfillmentStatusChanged,
            StorefrontContactForm,
        };
    }
}
