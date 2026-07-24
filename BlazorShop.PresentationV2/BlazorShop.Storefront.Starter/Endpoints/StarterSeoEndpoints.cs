namespace BlazorShop.Storefront.Starter.Endpoints
{
    using BlazorShop.Storefront.Starter.Options;

    using Microsoft.Extensions.Options;

    public static class StarterSeoEndpoints
    {
        public static IEndpointRouteBuilder MapStarterSeoEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/robots.txt", (IOptions<StarterStorefrontOptions> options) =>
            {
                var publicBaseUrl = ResolvePublicBaseUrl(options.Value);
                var sitemapUrl = CombineUrl(publicBaseUrl, "/sitemap.xml");
                var body = string.Join(
                    Environment.NewLine,
                    "User-agent: *",
                    "Allow: /",
                    $"Sitemap: {sitemapUrl}");

                return Results.Text(body, "text/plain");
            });

            endpoints.MapGet("/sitemap.xml", (IOptions<StarterStorefrontOptions> options) =>
            {
                var baseUrl = ResolvePublicBaseUrl(options.Value).TrimEnd('/');
                var urls = new[]
                {
                    "/",
                    "/category/sample-category",
                    "/product/sample-product",
                    "/content/about",
                };

                var entries = string.Join(
                    Environment.NewLine,
                    urls.Select(url => $"  <url><loc>{baseUrl}{url}</loc></url>"));

                var body = string.Join(
                    Environment.NewLine,
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                    "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">",
                    entries,
                    "</urlset>");

                return Results.Text(body, "application/xml");
            });

            return endpoints;
        }

        private static string CombineUrl(string baseUrl, string path)
        {
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private static string ResolvePublicBaseUrl(StarterStorefrontOptions options)
        {
            return string.IsNullOrWhiteSpace(options.PublicBaseUrl)
                ? "http://localhost"
                : options.PublicBaseUrl.Trim();
        }
    }
}
