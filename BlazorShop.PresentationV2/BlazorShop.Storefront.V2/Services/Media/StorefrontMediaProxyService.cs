namespace BlazorShop.Storefront.Services.Media
{
    using BlazorShop.Storefront.Configuration;

    public sealed class StorefrontMediaProxyService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;

        public StorefrontMediaProxyService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
        }

        public async Task<IResult> ProxyAsync(
            string mediaPath,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            var storeKey = StorefrontApiEndpointResolver.ResolveStoreKey(configuration);
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return Results.NotFound();
            }

            var client = httpClientFactory.CreateClient();
            var targetUri = new Uri(
                StorefrontApiEndpointResolver.ResolveCommerceNodeBaseAddress(configuration),
                $"{mediaPath}{httpContext.Request.QueryString}");

            using var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
            request.Headers.TryAddWithoutValidation("X-Store-Key", storeKey);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            CopyHeaderIfPresent(response, httpContext.Response, "Cache-Control");
            CopyHeaderIfPresent(response, httpContext.Response, "ETag");
            CopyHeaderIfPresent(response, httpContext.Response, "Last-Modified");
            CopyHeaderIfPresent(response, httpContext.Response, "X-Content-Type-Options");

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            return Results.File(content, contentType);
        }

        private static void CopyHeaderIfPresent(HttpResponseMessage source, HttpResponse destination, string headerName)
        {
            if (source.Headers.TryGetValues(headerName, out var values)
                || source.Content.Headers.TryGetValues(headerName, out values))
            {
                destination.Headers[headerName] = string.Join(",", values);
            }
        }
    }
}
