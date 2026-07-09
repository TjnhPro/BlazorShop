namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Models.Discovery;
    using BlazorShop.Web.Shared.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Web.Shared.Models.Category;
    using BlazorShop.Web.Shared.Models.Product;
    using BlazorShop.Web.Shared.Models.Seo;

    using Microsoft.Extensions.Options;

    public class StorefrontApiClient
    {
        // Static informational pages should degrade faster than catalog-backed pages when the API is offline.
        private static readonly TimeSpan CatalogRequestTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan RedirectResolutionRequestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan SeoSettingsRequestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string InternalCatalogBaseRoute = "internal/catalog";
        private const string InternalSeoBaseRoute = "internal/seo";
        private const string InternalCatalogSitemapRoute = InternalCatalogBaseRoute + "/sitemap";
        private const string InternalCategoriesRoute = InternalCatalogBaseRoute + "/categories";
        private const string InternalProductsRoute = InternalCatalogBaseRoute + "/products";
        private const string SeoSettingsRoute = InternalSeoBaseRoute + "/settings";
        private const string LegacyCatalogBaseRoute = "public/catalog";
        private const string LegacySeoRedirectsBaseRoute = "public/seo/redirects";
        private const string LegacyCatalogSitemapRoute = LegacyCatalogBaseRoute + "/sitemap";
        private const string LegacyCategoriesRoute = LegacyCatalogBaseRoute + "/categories";
        private const string LegacyProductsRoute = LegacyCatalogBaseRoute + "/products";
        private const string LegacySeoSettingsRoute = "seo/settings";

        private readonly HttpClient _httpClient;
        private readonly bool _enableLegacyFallback;

        public StorefrontApiClient(HttpClient httpClient, IOptions<StorefrontApiOptions> options)
        {
            _httpClient = httpClient;
            _enableLegacyFallback = options.Value.EnableLegacyFallback;
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsyncWithFallback<List<GetCategory>>(
                InternalCategoriesRoute,
                LegacyCategoriesRoute,
                cancellationToken,
                []);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetCategory>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetCategory>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<GetCategory>>.Success([]);
        }

        public Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                InternalCatalogSitemapRoute,
                LegacyCatalogSitemapRoute,
                cancellationToken,
                new GetPublicCatalogSitemap(),
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                BuildCatalogRoute(query, InternalProductsRoute),
                BuildCatalogRoute(query, LegacyProductsRoute),
                cancellationToken,
                new PagedResult<GetCatalogProduct>(),
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetCategoryPage>(
                $"{InternalCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}",
                $"{LegacyCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                $"{InternalProductsRoute}/slug/{Uri.EscapeDataString(slug)}",
                $"{LegacyProductsRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return Task.FromResult(StorefrontApiResult<GetProduct>.NotFound());
            }

            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                $"{InternalProductsRoute}/{id}",
                $"product/single/{id}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback<GetSeoSettings>(
                SeoSettingsRoute,
                LegacySeoSettingsRoute,
                cancellationToken,
                requestTimeout: SeoSettingsRequestTimeout);
        }

        public Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(string path, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<SeoRedirectResolutionDto>(
                $"{InternalSeoBaseRoute}/redirects/resolve?path={Uri.EscapeDataString(path)}",
                $"{LegacySeoRedirectsBaseRoute}/resolve?path={Uri.EscapeDataString(path)}",
                cancellationToken,
                RedirectResolutionRequestTimeout);
        }

        private async Task<StorefrontApiResult<T>> GetAsyncWithFallback<T>(
            string route,
            string fallbackRoute,
            CancellationToken cancellationToken,
            T? fallbackValue = default,
            TimeSpan? requestTimeout = null)
        {
            var result = await GetAsync<T>(route, cancellationToken, requestTimeout: requestTimeout);
            if (result.IsSuccess)
            {
                return result;
            }

            if (!_enableLegacyFallback)
            {
                return result;
            }

            try
            {
                return await GetAsync(fallbackRoute, cancellationToken, fallbackValue, requestTimeout);
            }
            catch (ObjectDisposedException)
            {
                return result;
            }
        }

        private async Task<StorefrontApiResult<T>> GetMaybeNotFoundWithFallbackAsync<T>(
            string route,
            string fallbackRoute,
            CancellationToken cancellationToken,
            TimeSpan requestTimeout)
        {
            var result = await GetMaybeNotFoundAsync<T>(route, cancellationToken, requestTimeout);
            if (result.IsSuccess)
            {
                return result;
            }

            if (!_enableLegacyFallback)
            {
                return result;
            }

            try
            {
                return await GetMaybeNotFoundAsync<T>(fallbackRoute, cancellationToken, requestTimeout);
            }
            catch (ObjectDisposedException)
            {
                return result;
            }
        }

        private async Task<StorefrontApiResult<T>> GetAsync<T>(string route, CancellationToken cancellationToken, T? fallbackValue = default, TimeSpan? requestTimeout = null)
        {
            using var requestTimeoutToken = CreateRequestTimeoutToken(cancellationToken, requestTimeout ?? CatalogRequestTimeout);

            try
            {
                using var response = await _httpClient.GetAsync(route, requestTimeoutToken.Token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return fallbackValue is not null
                        ? StorefrontApiResult<T>.Success(fallbackValue)
                        : StorefrontApiResult<T>.NotFound();
                }

                response.EnsureSuccessStatusCode();

                var payload = await ReadPayloadAsync<T>(response, requestTimeoutToken.Token);
                if (payload is not null)
                {
                    return StorefrontApiResult<T>.Success(payload);
                }

                return fallbackValue is not null
                    ? StorefrontApiResult<T>.Success(fallbackValue)
                    : StorefrontApiResult<T>.NotFound();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
        }

        private async Task<StorefrontApiResult<T>> GetMaybeNotFoundAsync<T>(string route, CancellationToken cancellationToken, TimeSpan requestTimeout)
        {
            using var requestTimeoutToken = CreateRequestTimeoutToken(cancellationToken, requestTimeout);

            try
            {
                using var response = await _httpClient.GetAsync(route, requestTimeoutToken.Token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return StorefrontApiResult<T>.NotFound();
                }

                response.EnsureSuccessStatusCode();

                var payload = await ReadPayloadAsync<T>(response, cancellationToken);
                return payload is not null
                    ? StorefrontApiResult<T>.Success(payload)
                    : StorefrontApiResult<T>.NotFound();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException)
            {
                return StorefrontApiResult<T>.ServiceUnavailable();
            }
        }

        private static CancellationTokenSource CreateRequestTimeoutToken(CancellationToken cancellationToken, TimeSpan requestTimeout)
        {
            var requestTimeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeoutToken.CancelAfter(requestTimeout);
            return requestTimeoutToken;
        }

        private static string BuildCatalogRoute(ProductCatalogQuery query, string productsRoute)
        {
            var parameters = new List<string>
            {
                $"pageNumber={Math.Max(1, query.PageNumber)}",
                $"pageSize={Math.Max(1, query.PageSize)}",
                $"sortBy={Uri.EscapeDataString(query.SortBy.ToString())}",
            };

            if (query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty)
            {
                parameters.Add($"categoryId={query.CategoryId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                parameters.Add($"searchTerm={Uri.EscapeDataString(query.SearchTerm.Trim())}");
            }

            if (query.CreatedAfterUtc.HasValue)
            {
                parameters.Add($"createdAfterUtc={Uri.EscapeDataString(query.CreatedAfterUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
            }

            return $"{productsRoute}?{string.Join("&", parameters)}";
        }

        private static async Task<T?> ReadPayloadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("success", out var successProperty)
                && document.RootElement.TryGetProperty("data", out var dataProperty))
            {
                if (successProperty.ValueKind == JsonValueKind.False)
                {
                    return default;
                }

                return dataProperty.ValueKind == JsonValueKind.Null
                    ? default
                    : dataProperty.Deserialize<T>(JsonOptions);
            }

            return document.RootElement.Deserialize<T>(JsonOptions);
        }
    }
}
