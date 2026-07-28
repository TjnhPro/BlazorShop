namespace BlazorShop.Storefront.Options
{
    public sealed class StorefrontRuntimeBindingOptions
    {
        public const string SectionName = "Storefront";

        public string? CommerceNodeBaseUrl { get; set; }

        public string? StoreKey { get; set; }

        public string? PublicBaseUrl { get; set; }
    }
}
