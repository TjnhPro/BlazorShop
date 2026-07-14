namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class PaymentAttemptStates
    {
        public const string Created = "created";
        public const string RequiresAction = "requires_action";
        public const string Authorized = "authorized";
        public const string Captured = "captured";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";
        public const string Expired = "expired";
    }
}
