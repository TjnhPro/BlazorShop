namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class CommerceTaskWorkerOptions
    {
        public const string SectionName = "CommerceTaskWorker";

        public bool Enabled { get; set; } = true;

        public int PollIntervalSeconds { get; set; } = 5;

        public int RetryDelaySeconds { get; set; } = 30;

        public string? WorkerId { get; set; }
    }
}
