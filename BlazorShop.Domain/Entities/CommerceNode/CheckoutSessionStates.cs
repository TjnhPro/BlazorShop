namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class CheckoutSessionStates
    {
        public const string Draft = "draft";

        public const string Ready = "ready";

        public const string OrderPending = "order_pending";

        public const string Completed = "completed";

        public const string Expired = "expired";

        public const string Cancelled = "cancelled";
    }
}
