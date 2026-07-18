namespace BlazorShop.Storefront.Configuration
{
    using BlazorShop.Storefront.Options;

    public static class StorefrontApiEndpointResolver
    {
        public static void ConfigureStorefrontHttpClient(HttpClient client, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(configuration);

            client.BaseAddress = ResolveScopedStorefrontApiBaseAddress(configuration);
        }

        public static Uri ResolveApiBaseAddress(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var configuredBaseAddress = configuration[$"{StorefrontApiOptions.SectionName}:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredBaseAddress)
                && Uri.TryCreate(configuredBaseAddress, UriKind.Absolute, out var configuredUri))
            {
                return configuredUri;
            }

            return new Uri("https+http://apiservice/api/");
        }

        public static Uri ResolveCommerceNodeBaseAddress(IConfiguration configuration)
        {
            var apiBaseAddress = ResolveApiBaseAddress(configuration);
            return new UriBuilder(apiBaseAddress)
            {
                Path = "/",
                Query = string.Empty,
                Fragment = string.Empty,
            }.Uri;
        }

        public static Uri ResolveScopedStorefrontApiBaseAddress(IConfiguration configuration)
        {
            var apiBaseAddress = ResolveApiBaseAddress(configuration);
            var storeKey = ResolveStoreKey(configuration);
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return apiBaseAddress;
            }

            var path = apiBaseAddress.AbsolutePath.TrimEnd('/')
                + "/storefront/stores/"
                + Uri.EscapeDataString(storeKey)
                + "/";

            return new UriBuilder(apiBaseAddress)
            {
                Path = path,
                Query = string.Empty,
                Fragment = string.Empty,
            }.Uri;
        }

        public static string? ResolveStoreKey(IConfiguration configuration)
        {
            return StorefrontStoreKeyResolver.Resolve(configuration);
        }
    }
}
