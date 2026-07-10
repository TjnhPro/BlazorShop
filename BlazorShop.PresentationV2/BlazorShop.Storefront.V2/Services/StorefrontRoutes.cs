namespace BlazorShop.Storefront.Services
{
    public static class StorefrontRoutes
    {
        public const string Home = "/";
        public const string Cart = "/my-cart";
        public const string Checkout = "/checkout";
        public const string SignIn = "/signin";
        public const string Register = "/register";
        public const string Logout = "/logout";
        public const string Sitemap = "/sitemap.xml";
        public const string Robots = "/robots.txt";
        public const string About = "/about-us";
        public const string Faq = "/faq";
        public const string Privacy = "/privacy";
        public const string Terms = "/terms";
        public const string CustomerService = "/customer-service";
        public const string NewReleases = "/new-releases";
        public const string TodaysDeals = "/todays-deals";
        public const string Search = "/search";

        public static IReadOnlyList<StorefrontSitemapStaticRoute> SitemapStaticPages { get; } =
        [
            new(Home, UseCatalogLastModified: true),
            new(About),
            new(Faq),
            new(Privacy),
            new(Terms),
            new(CustomerService),
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
