namespace BlazorShop.Storefront.Options
{
    public sealed class StorefrontApiOptions
    {
        public const string SectionName = "Api";

        public string? BaseUrl { get; set; }

        public string? StoreKey { get; set; }

        public bool EnableLegacyFallback { get; set; }
    }
}
