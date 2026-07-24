namespace BlazorShop.Storefront.Services
{

    using BlazorShop.Storefront.Models;
using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public partial class StorefrontApiClient
    {
        private const string StorefrontCatalogBaseRoute = "catalog";
        private const string StorefrontNavigationBaseRoute = "navigation";
        private const string StorefrontPagesBaseRoute = "pages";
        private const string StorefrontSeoBaseRoute = "seo";
        private const string StorefrontConfigurationRoute = "configuration";
        private const string StorefrontConsentCurrentRoute = "consent/current";
        private const string StorefrontConsentRoute = "consent";
        private const string StorefrontConsentRevokeRoute = "consent/revoke";
        private const string StorefrontAddressBaseRoute = "address";
        private const string StorefrontAddressCountriesRoute = StorefrontAddressBaseRoute + "/countries";
        private const string StorefrontAddressConfigurationRoute = StorefrontAddressBaseRoute + "/configuration";
        private const string StorefrontCustomerAddressesRoute = "customer/addresses";
        private const string StorefrontCustomerProfileRoute = "customer/profile";
        private const string StorefrontCustomerOrdersRoute = "orders/current-user";
        private const string StorefrontStoreCurrentRoute = "store/current";
        private const string StorefrontPaymentMethodsRoute = "payments/methods";
        private const string StorefrontCheckoutStartRoute = "checkout/start";
        private const string StorefrontCheckoutPreviewRoute = "checkout/preview";
        private const string StorefrontPlaceOrderRoute = "checkout/place-order";
        private const string StorefrontPaymentAttemptsRoute = "payments/attempts";
        private const string StorefrontCurrencyPreferenceRoute = "currency/preference";
        private const string StorefrontCartRoute = "cart";
        private const string StorefrontCartSessionRoute = StorefrontCartRoute + "/session";
        private const string StorefrontCartLinesRoute = StorefrontCartRoute + "/lines";
        private const string StorefrontCartRecalculateRoute = StorefrontCartRoute + "/recalculate";
        private const string StorefrontCartMergeCurrentCustomerRoute = StorefrontCartRoute + "/merge-current-customer";
        private const string CartTokenHeaderName = "X-Cart-Token";
        private const string ConsentVisitorHeaderName = "X-Consent-Visitor";
        private const string StorefrontCatalogSitemapRoute = StorefrontCatalogBaseRoute + "/sitemap";
        private const string StorefrontCategoriesRoute = StorefrontCatalogBaseRoute + "/categories";
        private const string StorefrontCategoryTreeRoute = StorefrontCategoriesRoute + "/tree";
        private const string StorefrontProductFilterMetadataRoute = StorefrontCatalogBaseRoute + "/product-filter-metadata";
        private const string StorefrontSearchSuggestionsRoute = StorefrontCatalogBaseRoute + "/search-suggestions";
        private const string StorefrontProductsRoute = StorefrontCatalogBaseRoute + "/products";
        private const string SeoSettingsRoute = StorefrontSeoBaseRoute + "/settings";
        private const string LegacyCatalogBaseRoute = "/api/public/catalog";
        private const string LegacySeoRedirectsBaseRoute = "/api/public/seo/redirects";
        private const string LegacyCatalogSitemapRoute = LegacyCatalogBaseRoute + "/sitemap";
        private const string LegacyCategoriesRoute = LegacyCatalogBaseRoute + "/categories";
        private const string LegacyProductsRoute = LegacyCatalogBaseRoute + "/products";
        private const string LegacySeoSettingsRoute = "/api/seo/settings";
        private static string BuildCatalogRoute(ProductCatalogQuery query, string productsRoute, string? currencyCode = null)
        {
            var parameters = new List<string>
            {
                $"pageNumber={Math.Max(1, query.PageNumber)}",
                $"pageSize={Math.Max(1, query.PageSize)}",
                $"sortBy={Uri.EscapeDataString(query.SortBy.ToApiValue())}",
            };

            if (query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty)
            {
                parameters.Add($"categoryId={query.CategoryId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            {
                parameters.Add($"categorySlug={Uri.EscapeDataString(query.CategorySlug.Trim())}");
            }

            if (query.IncludeSubcategories)
            {
                parameters.Add("includeSubcategories=true");
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                parameters.Add($"searchTerm={Uri.EscapeDataString(query.SearchTerm.Trim())}");
            }

            if (query.MinPrice.HasValue)
            {
                parameters.Add($"minPrice={Uri.EscapeDataString(query.MinPrice.Value.ToString(CultureInfo.InvariantCulture))}");
            }

            if (query.MaxPrice.HasValue)
            {
                parameters.Add($"maxPrice={Uri.EscapeDataString(query.MaxPrice.Value.ToString(CultureInfo.InvariantCulture))}");
            }

            if (query.InStock.HasValue)
            {
                parameters.Add($"inStock={query.InStock.Value}");
            }

            if (query.CreatedAfterUtc.HasValue)
            {
                parameters.Add($"createdAfterUtc={Uri.EscapeDataString(query.CreatedAfterUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
            }

            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCurrencyCode is not null)
            {
                parameters.Add($"currencyCode={Uri.EscapeDataString(normalizedCurrencyCode)}");
            }

            return $"{productsRoute}?{string.Join("&", parameters)}";
        }
        private static string BuildProductFilterMetadataRoute(string? categorySlug, string? searchTerm, string? currencyCode)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                parameters.Add($"categorySlug={Uri.EscapeDataString(categorySlug.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                parameters.Add($"searchTerm={Uri.EscapeDataString(searchTerm.Trim())}");
            }

            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCurrencyCode is not null)
            {
                parameters.Add($"currencyCode={Uri.EscapeDataString(normalizedCurrencyCode)}");
            }

            return parameters.Count == 0
                ? StorefrontProductFilterMetadataRoute
                : $"{StorefrontProductFilterMetadataRoute}?{string.Join("&", parameters)}";
        }
        private static string BuildSearchSuggestionsRoute(string? searchTerm, string? categorySlug, int? limit, string? currencyCode)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                parameters.Add($"searchTerm={Uri.EscapeDataString(searchTerm.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                parameters.Add($"categorySlug={Uri.EscapeDataString(categorySlug.Trim())}");
            }

            if (limit is > 0)
            {
                parameters.Add($"limit={limit.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCurrencyCode is not null)
            {
                parameters.Add($"currencyCode={Uri.EscapeDataString(normalizedCurrencyCode)}");
            }

            return parameters.Count == 0
                ? StorefrontSearchSuggestionsRoute
                : $"{StorefrontSearchSuggestionsRoute}?{string.Join("&", parameters)}";
        }
        private static string AppendCurrencyQuery(string route, string? currencyCode)
        {
            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCurrencyCode is null)
            {
                return route;
            }

            var separator = route.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{route}{separator}currencyCode={Uri.EscapeDataString(normalizedCurrencyCode)}";
        }
        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }
        private static string? NormalizeCountryCode(string? countryCode)
        {
            var normalized = countryCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 2 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }
        private static string? NormalizeOrderReference(string? orderReference)
        {
            return string.IsNullOrWhiteSpace(orderReference)
                ? null
                : orderReference.Trim();
        }
    }
}
