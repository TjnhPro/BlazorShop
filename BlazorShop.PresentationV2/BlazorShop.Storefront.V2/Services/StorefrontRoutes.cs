namespace BlazorShop.Storefront.Services
{
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

        public static string SearchUrl(string? query, string? categorySlug = null, int? pageNumber = null)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(query))
            {
                parameters.Add($"q={Uri.EscapeDataString(query.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                parameters.Add($"category={Uri.EscapeDataString(categorySlug.Trim())}");
            }

            if (pageNumber.HasValue && pageNumber.Value > 1)
            {
                parameters.Add($"page={pageNumber.Value}");
            }

            return parameters.Count == 0
                ? Search
                : $"{Search}?{string.Join("&", parameters)}";
        }
    }

    public sealed record StorefrontSitemapStaticRoute(string Path, bool UseCatalogLastModified = false);
}
