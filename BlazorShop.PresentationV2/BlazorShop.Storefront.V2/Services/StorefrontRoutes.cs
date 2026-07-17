namespace BlazorShop.Storefront.Services
{
    using System.Globalization;

    using BlazorShop.Web.SharedV2.Models.Product;

    public static class StorefrontRoutes
    {
        public const string Home = "/";
        public const string HomeMetadataSlug = "home";
        public const string Maintenance = "/maintenance";
        public const string Cart = "/my-cart";
        public const string Checkout = "/checkout";
        public const string PaymentSuccess = "/payment-success";
        public const string PaymentCancel = "/payment-cancel";
        public const string SignIn = "/signin";
        public const string Register = "/register";
        public const string Logout = "/logout";
        public const string Account = "/account";
        public const string AccountProfile = "/account/profile";
        public const string AccountChangePassword = "/account/change-password";
        public const string CurrencyPreference = "/currency";
        public const string Sitemap = "/sitemap.xml";
        public const string Robots = "/robots.txt";
        public const string About = "/pages/about-us";
        public const string Faq = "/pages/faq";
        public const string Privacy = "/pages/privacy";
        public const string Terms = "/pages/terms";
        public const string CustomerService = "/pages/customer-service";
        public const string NewReleases = "/new-releases";
        public const string TodaysDeals = "/todays-deals";
        public const string Search = "/search";
        public const string PagesBase = "/pages";

        public static IReadOnlyList<StorefrontSitemapStaticRoute> SitemapStaticPages { get; } =
        [
            new(Home, UseCatalogLastModified: true),
            new(NewReleases, UseCatalogLastModified: true),
            new(TodaysDeals, UseCatalogLastModified: true),
        ];

        public static string Category(string? slug)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? "/category"
                : $"/category/{Uri.EscapeDataString(slug.Trim())}";
        }

        public static string Product(string? slug)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? "/product"
                : $"/product/{Uri.EscapeDataString(slug.Trim())}";
        }

        public static string Page(string? slug)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? PagesBase
                : $"{PagesBase}/{Uri.EscapeDataString(slug.Trim())}";
        }

        public static string CategoryUrl(
            string? slug,
            int? pageNumber = null,
            int? pageSize = null,
            ProductCatalogSortBy? sortBy = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null)
        {
            var parameters = BuildCatalogQueryParameters(pageNumber, pageSize, sortBy, minPrice, maxPrice, inStock);
            var route = Category(slug);

            return parameters.Count == 0
                ? route
                : $"{route}?{string.Join("&", parameters)}";
        }

        public static string SearchUrl(
            string? query,
            string? categorySlug = null,
            int? pageNumber = null,
            int? pageSize = null,
            ProductCatalogSortBy? sortBy = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null)
        {
            var parameters = BuildCatalogQueryParameters(pageNumber, pageSize, sortBy, minPrice, maxPrice, inStock);

            if (!string.IsNullOrWhiteSpace(query))
            {
                parameters.Insert(0, $"q={Uri.EscapeDataString(query.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                parameters.Insert(string.IsNullOrWhiteSpace(query) ? 0 : 1, $"category={Uri.EscapeDataString(categorySlug.Trim())}");
            }

            return parameters.Count == 0
                ? Search
                : $"{Search}?{string.Join("&", parameters)}";
        }

        private static List<string> BuildCatalogQueryParameters(
            int? pageNumber,
            int? pageSize,
            ProductCatalogSortBy? sortBy,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock)
        {
            var parameters = new List<string>();

            if (pageNumber.HasValue && pageNumber.Value > 1)
            {
                parameters.Add($"page={pageNumber.Value}");
            }

            if (pageSize.HasValue && pageSize.Value > 0)
            {
                parameters.Add($"pageSize={pageSize.Value}");
            }

            if (sortBy.HasValue)
            {
                parameters.Add($"sortBy={Uri.EscapeDataString(sortBy.Value.ToApiValue())}");
            }

            if (minPrice.HasValue)
            {
                parameters.Add($"minPrice={Uri.EscapeDataString(minPrice.Value.ToString(CultureInfo.InvariantCulture))}");
            }

            if (maxPrice.HasValue)
            {
                parameters.Add($"maxPrice={Uri.EscapeDataString(maxPrice.Value.ToString(CultureInfo.InvariantCulture))}");
            }

            if (inStock == true)
            {
                parameters.Add("inStock=true");
            }

            return parameters;
        }
    }

    public sealed record StorefrontSitemapStaticRoute(string Path, bool UseCatalogLastModified = false);
}
