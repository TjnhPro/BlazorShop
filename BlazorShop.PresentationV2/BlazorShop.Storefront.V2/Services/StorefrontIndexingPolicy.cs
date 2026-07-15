namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Application.DTOs.Seo;

    public static class StorefrontIndexingPolicy
    {
        public const string NoIndexNoFollow = "noindex, nofollow";
        public const string NoIndexFollow = "noindex,follow";

        private static readonly HashSet<string> PrivateNoIndexPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            StorefrontRoutes.Cart,
            StorefrontRoutes.Checkout,
            StorefrontRoutes.SignIn,
            StorefrontRoutes.Register,
            StorefrontRoutes.Logout,
            StorefrontRoutes.PaymentSuccess,
            StorefrontRoutes.PaymentCancel,
            StorefrontRoutes.CurrencyPreference,
            StorefrontRoutes.Maintenance,
        };

        public static bool IsPrivateNoIndexPath(string? path)
        {
            var normalized = NormalizePath(path);
            return normalized is not null && PrivateNoIndexPaths.Contains(normalized);
        }

        public static bool IsSearchNoIndexPath(string? path)
        {
            return string.Equals(NormalizePath(path), StorefrontRoutes.Search, StringComparison.OrdinalIgnoreCase);
        }

        public static string? NormalizeCanonicalPath(string? path)
        {
            var normalized = NormalizePath(path);
            if (normalized is null)
            {
                return null;
            }

            return normalized == StorefrontRoutes.Home ? StorefrontRoutes.Home : normalized.TrimEnd('/');
        }

        public static void ApplySearchMetadata(SeoMetadataDto metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);

            metadata.RobotsIndex = false;
            metadata.RobotsFollow = true;
            metadata.CanonicalUrl = null;
        }

        private static string? NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var normalized = path.Trim();
            var queryIndex = normalized.IndexOfAny(['?', '#']);
            if (queryIndex >= 0)
            {
                normalized = normalized[..queryIndex];
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = $"/{normalized}";
            }

            return normalized == StorefrontRoutes.Home ? StorefrontRoutes.Home : normalized.TrimEnd('/');
        }
    }
}
