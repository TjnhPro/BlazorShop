namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class CommerceNodeRuntimeOptions
    {
        public const string SectionName = "Runtime";

        public CommerceNodeSecurityOptions Security { get; set; } = new();

        public CommerceNodeRateLimitingOptions RateLimiting { get; set; } = new();
    }

    public sealed class CommerceNodeSecurityOptions
    {
        public string RegistrationMode { get; set; } = "standard";

        public string RefreshTokenCookieName { get; set; } = "__Host-blazorshop-refresh";

        public string RefreshTokenCookieSameSite { get; set; } = "Strict";

        public int RefreshTokenLifetimeDays { get; set; } = 14;
    }

    public sealed class CommerceNodeRateLimitingOptions
    {
        public bool Enabled { get; set; } = true;

        public CommerceNodeRateLimitPolicyOptions AuthStrict { get; set; } = new()
        {
            PermitLimit = 20,
            WindowSeconds = 60,
        };

        public CommerceNodeRateLimitPolicyOptions Cart { get; set; } = new()
        {
            PermitLimit = 180,
            WindowSeconds = 60,
        };

        public CommerceNodeRateLimitPolicyOptions Checkout { get; set; } = new()
        {
            PermitLimit = 60,
            WindowSeconds = 60,
        };

        public CommerceNodeRateLimitPolicyOptions Newsletter { get; set; } = new()
        {
            PermitLimit = 20,
            WindowSeconds = 300,
        };

        public CommerceNodeRateLimitPolicyOptions Currency { get; set; } = new()
        {
            PermitLimit = 120,
            WindowSeconds = 60,
        };
    }

    public sealed class CommerceNodeRateLimitPolicyOptions
    {
        public int PermitLimit { get; set; } = 120;

        public int WindowSeconds { get; set; } = 60;

        public int QueueLimit { get; set; }
    }
}
