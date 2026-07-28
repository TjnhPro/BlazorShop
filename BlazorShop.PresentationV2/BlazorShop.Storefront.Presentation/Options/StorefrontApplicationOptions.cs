namespace BlazorShop.Storefront.Options
{
    public sealed class StorefrontApplicationOptions
    {
        public const string SectionName = "Storefront:Application";

        public bool EnableInteractiveWebAssembly { get; set; } = true;

        public string FaviconRedirectPath { get; set; } = "/icon-192.png";
    }
}
