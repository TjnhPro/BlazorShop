namespace BlazorShop.Storefront.Options
{
    public sealed class StorefrontStoreResolutionOptions
    {
        public const string SectionName = "StoreResolution";

        public bool? RequireCurrentStore { get; set; }

        public static bool IsCurrentStoreRequired(StorefrontStoreResolutionOptions options, IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return options.RequireCurrentStore ?? !hostEnvironment.IsDevelopment();
        }
    }
}
