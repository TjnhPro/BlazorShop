namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.SharedV2.Models.Discovery;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;

    public class StorefrontApiClient
    {
        // Static informational pages should degrade faster than catalog-backed pages when the API is offline.
        private static readonly TimeSpan CatalogRequestTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan RedirectResolutionRequestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan SeoSettingsRequestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string StorefrontCatalogBaseRoute = "catalog";
        private const string StorefrontPagesBaseRoute = "pages";
        private const string StorefrontSeoBaseRoute = "seo";
        private const string StorefrontStoreCurrentRoute = "store/current";
        private const string StorefrontPaymentMethodsRoute = "payments/methods";
        private const string StorefrontCheckoutRoute = "cart/checkout";
        private const string StorefrontCatalogSitemapRoute = StorefrontCatalogBaseRoute + "/sitemap";
        private const string StorefrontCategoriesRoute = StorefrontCatalogBaseRoute + "/categories";
        private const string StorefrontCategoryTreeRoute = StorefrontCategoriesRoute + "/tree";
        private const string StorefrontProductsRoute = StorefrontCatalogBaseRoute + "/products";
        private const string SeoSettingsRoute = StorefrontSeoBaseRoute + "/settings";
        private const string LegacyCatalogBaseRoute = "/api/public/catalog";
        private const string LegacySeoRedirectsBaseRoute = "/api/public/seo/redirects";
        private const string LegacyCatalogSitemapRoute = LegacyCatalogBaseRoute + "/sitemap";
        private const string LegacyCategoriesRoute = LegacyCatalogBaseRoute + "/categories";
        private const string LegacyProductsRoute = LegacyCatalogBaseRoute + "/products";
        private const string LegacySeoSettingsRoute = "/api/seo/settings";

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
                StorefrontCategoriesRoute,
                LegacyCategoriesRoute,
                cancellationToken,
                []);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetCategory>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetCategory>>.ServiceUnavailable()
                : StorefrontApiResult<IReadOnlyList<GetCategory>>.Success([]);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<GetCategoryTreeNode>>(
                StorefrontCategoryTreeRoute,
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.Success([]);
        }

        public Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                StorefrontCatalogSitemapRoute,
                LegacyCatalogSitemapRoute,
                cancellationToken,
                new GetPublicCatalogSitemap(),
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                BuildCatalogRoute(query, StorefrontProductsRoute),
                BuildCatalogRoute(query, LegacyProductsRoute),
                cancellationToken,
                new PagedResult<GetCatalogProduct>(),
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetCategoryPage>(
                $"{StorefrontCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}",
                $"{LegacyCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                $"{StorefrontProductsRoute}/slug/{Uri.EscapeDataString(slug)}",
                $"{LegacyProductsRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<GetStorefrontPage>(
                $"{StorefrontPagesBaseRoute}/{Uri.EscapeDataString(slug)}",
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
                $"{StorefrontProductsRoute}/{id}",
                $"/api/product/single/{id}",
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
                $"{StorefrontSeoBaseRoute}/redirects/resolve?path={Uri.EscapeDataString(path)}",
                $"{LegacySeoRedirectsBaseRoute}/resolve?path={Uri.EscapeDataString(path)}",
                cancellationToken,
                RedirectResolutionRequestTimeout);
        }

        public Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<StorefrontCurrentStore>(
                StorefrontStoreCurrentRoute,
                cancellationToken,
                CatalogRequestTimeout);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<GetPaymentMethod>>(
                StorefrontPaymentMethodsRoute,
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<GetPaymentMethod>>.Success([]);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutResult>> CheckoutAsync(
            StorefrontCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            return PostAsync<StorefrontCheckoutRequest, StorefrontCheckoutResult>(
                StorefrontCheckoutRoute,
                request,
                "Unable to place order right now.",
                cancellationToken);
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

            if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            {
                parameters.Add($"categorySlug={Uri.EscapeDataString(query.CategorySlug.Trim())}");
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

        private async Task<StorefrontSubmitResult<TData>> PostAsync<TRequest, TData>(
            string route,
            TRequest request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(route, request, JsonOptions, cancellationToken);
                var envelope = await ReadEnvelopeAsync<TData>(response, cancellationToken);
                if (envelope is not null)
                {
                    return envelope.Success
                        ? StorefrontSubmitResult<TData>.Succeeded(envelope.Data, envelope.Message)
                        : StorefrontSubmitResult<TData>.Failed(envelope.Message);
                }

                return response.IsSuccessStatusCode
                    ? StorefrontSubmitResult<TData>.Succeeded(default, "Request completed.")
                    : StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return StorefrontSubmitResult<TData>.Failed(unavailableMessage);
            }
        }

        private static async Task<StorefrontApiEnvelope<TData>?> ReadEnvelopeAsync<TData>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            return JsonSerializer.Deserialize<StorefrontApiEnvelope<TData>>(payload, JsonOptions);
        }

        private sealed record StorefrontApiEnvelope<TData>(bool Success, string? Message, TData? Data);
    }

    public sealed record StorefrontSubmitResult<TData>(bool Success, string Message, TData? Data)
    {
        public static StorefrontSubmitResult<TData> Succeeded(TData? data, string? message)
        {
            return new(true, string.IsNullOrWhiteSpace(message) ? "Request completed." : message, data);
        }

        public static StorefrontSubmitResult<TData> Failed(string? message)
        {
            return new(false, string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message, default);
        }
    }

    public sealed record StorefrontCurrentStore(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps,
        string? CdnHost,
        string? LogoUrl,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string DefaultCurrencyCode,
        string DefaultCulture,
        string? SupportEmail,
        string? SupportPhone,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage,
        string? HtmlBodyId);
}
