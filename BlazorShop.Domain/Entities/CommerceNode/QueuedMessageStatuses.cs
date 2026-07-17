namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class QueuedMessageStatuses
    {
        public const string Pending = "pending";
        public const string Sending = "sending";
        public const string Sent = "sent";
        public const string WaitingRetry = "waiting_retry";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Pending,
            Sending,
            Sent,
            WaitingRetry,
            Failed,
            Cancelled,
        };
    }
}
