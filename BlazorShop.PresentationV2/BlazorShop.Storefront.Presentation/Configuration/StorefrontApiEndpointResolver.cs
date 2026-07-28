namespace BlazorShop.Storefront.Presentation.Configuration
{
    using BlazorShop.Storefront.Presentation.Options;
    using Microsoft.Extensions.Configuration;

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

            var storefrontCommerceNodeBaseAddress = configuration[$"{StorefrontRuntimeBindingOptions.SectionName}:CommerceNodeBaseUrl"];
            if (!string.IsNullOrWhiteSpace(storefrontCommerceNodeBaseAddress)
                && Uri.TryCreate(storefrontCommerceNodeBaseAddress, UriKind.Absolute, out var storefrontUri))
            {
                return storefrontUri;
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

        public static string? ResolvePublicBaseUrl(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return string.IsNullOrWhiteSpace(configuration[$"{StorefrontRuntimeBindingOptions.SectionName}:PublicBaseUrl"])
                ? null
                : configuration[$"{StorefrontRuntimeBindingOptions.SectionName}:PublicBaseUrl"]!.Trim();
        }
    }
}
