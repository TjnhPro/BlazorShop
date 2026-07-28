namespace BlazorShop.Storefront.Options
{
    public sealed class StorefrontRateLimitingOptions
    {
        public const string SectionName = "Storefront:RateLimiting";

        public bool Enabled { get; set; } = true;

        public StorefrontRateLimitPolicyOptions Cart { get; set; } = new()
        {
            PermitLimit = 180,
            WindowSeconds = 60,
        };
    }

    public sealed class StorefrontRateLimitPolicyOptions
    {
        public int PermitLimit { get; set; } = 120;

        public int WindowSeconds { get; set; } = 60;

        public int QueueLimit { get; set; }
    }
}
