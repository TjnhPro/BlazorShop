namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class CommerceTaskStatuses
    {
        public const string Pending = "pending";

        public const string Running = "running";

        public const string WaitingRetry = "waiting_retry";

        public const string Succeeded = "succeeded";

        public const string Failed = "failed";

        public const string Cancelled = "cancelled";

        public const string Dead = "dead";
    }
}
