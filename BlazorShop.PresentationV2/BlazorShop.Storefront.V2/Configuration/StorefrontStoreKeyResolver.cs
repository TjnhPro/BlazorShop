namespace BlazorShop.Storefront.Configuration
{
    using BlazorShop.Storefront.Options;

    public static class StorefrontStoreKeyResolver
    {
        public static string? Resolve(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return FirstNonEmpty(
                configuration[$"{StorefrontApiOptions.SectionName}:StoreKey"],
                configuration["StoreKey"],
                configuration["STORE_KEY"]);
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }
    }
}
