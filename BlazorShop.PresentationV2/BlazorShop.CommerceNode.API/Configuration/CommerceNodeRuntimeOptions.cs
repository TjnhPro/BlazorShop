namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class CommerceNodeRuntimeOptions
    {
        public const string SectionName = "Runtime";

        public CommerceNodeSecurityOptions Security { get; set; } = new();
    }

    public sealed class CommerceNodeSecurityOptions
    {
        public string RefreshTokenCookieName { get; set; } = "__Host-blazorshop-refresh";

        public string RefreshTokenCookieSameSite { get; set; } = "Strict";

        public int RefreshTokenLifetimeDays { get; set; } = 14;
    }
}
