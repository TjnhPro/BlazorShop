namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class CommerceTaskStepStatuses
    {
        public const string Pending = "pending";

        public const string Running = "running";

        public const string Succeeded = "succeeded";

        public const string Failed = "failed";

        public const string Skipped = "skipped";

        public const string RolledBack = "rolled_back";
    }
}
