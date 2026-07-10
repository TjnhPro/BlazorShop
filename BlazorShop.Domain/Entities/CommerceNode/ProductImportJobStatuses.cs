namespace BlazorShop.Domain.Entities.CommerceNode
{
    public static class ProductImportJobStatuses
    {
        public const string Queued = "Queued";

        public const string Running = "Running";

        public const string Completed = "Completed";

        public const string CompletedWithErrors = "CompletedWithErrors";

        public const string Failed = "Failed";
    }
}
