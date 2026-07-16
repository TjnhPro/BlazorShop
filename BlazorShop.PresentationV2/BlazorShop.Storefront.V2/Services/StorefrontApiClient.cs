namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
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
        private const string StorefrontNavigationBaseRoute = "navigation";
        private const string StorefrontPagesBaseRoute = "pages";
        private const string StorefrontSeoBaseRoute = "seo";
        private const string StorefrontConfigurationRoute = "configuration";
        private const string StorefrontConsentCurrentRoute = "consent/current";
        private const string StorefrontConsentRoute = "consent";
        private const string StorefrontConsentRevokeRoute = "consent/revoke";
        private const string StorefrontStoreCurrentRoute = "store/current";
        private const string StorefrontPaymentMethodsRoute = "payments/methods";
        private const string StorefrontCheckoutPreviewRoute = "checkout/preview";
        private const string StorefrontPlaceOrderRoute = "checkout/place-order";
        private const string StorefrontPaymentAttemptsRoute = "payments/attempts";
        private const string StorefrontCurrencyPreferenceRoute = "currency/preference";
        private const string StorefrontCartRoute = "cart";
        private const string StorefrontCartSessionRoute = StorefrontCartRoute + "/session";
        private const string StorefrontCartLinesRoute = StorefrontCartRoute + "/lines";
        private const string CartTokenHeaderName = "X-Cart-Token";
        private const string ConsentVisitorHeaderName = "X-Consent-Visitor";
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
            return GetPublishedCatalogPageAsync(query, currencyCode: null, cancellationToken);
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
            ProductCatalogQuery query,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                BuildCatalogRoute(query, StorefrontProductsRoute, currencyCode),
                BuildCatalogRoute(query, LegacyProductsRoute),
                cancellationToken,
                new PagedResult<GetCatalogProduct>(),
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetPublishedCategoryBySlugAsync(slug, currencyCode: null, cancellationToken);
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetCategoryPage>(
                AppendCurrencyQuery($"{StorefrontCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}", currencyCode),
                $"{LegacyCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetPublishedProductBySlugAsync(slug, currencyCode: null, cancellationToken);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                AppendCurrencyQuery($"{StorefrontProductsRoute}/slug/{Uri.EscapeDataString(slug)}", currencyCode),
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

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<StorefrontPageNavigationLinkDto>>(
                $"{StorefrontPagesBaseRoute}/navigation",
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.Success([]);
        }

        public Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return Task.FromResult(StorefrontApiResult<StoreNavigationPublicMenuDto>.NotFound());
            }

            return GetMaybeNotFoundAsync<StoreNavigationPublicMenuDto>(
                $"{StorefrontNavigationBaseRoute}/{Uri.EscapeDataString(systemName.Trim().ToLowerInvariant())}",
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetProductByIdAsync(id, currencyCode: null, cancellationToken);
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(
            Guid id,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return Task.FromResult(StorefrontApiResult<GetProduct>.NotFound());
            }

            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                AppendCurrencyQuery($"{StorefrontProductsRoute}/{id}", currencyCode),
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

        public Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<StorefrontPublicConfiguration>(
                StorefrontConfigurationRoute,
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontSubmitResult<StorefrontConsentState>> GetConsentAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default)
        {
            return SendConsentAsync<StorefrontConsentState>(
                HttpMethod.Get,
                StorefrontConsentCurrentRoute,
                visitorKey,
                request: null,
                "Unable to load consent state right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontConsentState>> SaveConsentAsync(
            string visitorKey,
            StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendConsentAsync<StorefrontConsentState>(
                HttpMethod.Post,
                StorefrontConsentRoute,
                visitorKey,
                request,
                "Unable to save consent right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontConsentState>> RevokeConsentAsync(
            string visitorKey,
            CancellationToken cancellationToken = default)
        {
            return SendConsentAsync<StorefrontConsentState>(
                HttpMethod.Post,
                StorefrontConsentRevokeRoute,
                visitorKey,
                request: null,
                "Unable to revoke consent right now.",
                cancellationToken);
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

        public Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
            StorefrontCurrencyPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            return PostAsync<StorefrontCurrencyPreferenceRequest, StorefrontCurrencyPreferenceResponse>(
                StorefrontCurrencyPreferenceRoute,
                request,
                "Unable to update currency preference right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutPreviewResponse>> PreviewCheckoutAsync(
            string cartToken,
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCheckoutPreviewResponse>(
                HttpMethod.Post,
                StorefrontCheckoutPreviewRoute,
                cartToken,
                request,
                "Unable to preview checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontPlaceOrderResponse>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return PostAsync<StorefrontPlaceOrderRequest, StorefrontPlaceOrderResponse>(
                StorefrontPlaceOrderRoute,
                request,
                "Unable to place order right now.",
                cancellationToken);
        }

        public Task<StorefrontApiResult<StorefrontPaymentAttemptResponse>> GetPaymentAttemptAsync(
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            if (paymentAttemptId == Guid.Empty)
            {
                return Task.FromResult(StorefrontApiResult<StorefrontPaymentAttemptResponse>.NotFound());
            }

            return GetAsync<StorefrontPaymentAttemptResponse>(
                $"{StorefrontPaymentAttemptsRoute}/{paymentAttemptId:D}",
                cancellationToken,
                fallbackValue: null,
                CatalogRequestTimeout);
        }

        public Task<StorefrontSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeCartSessionAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            return PostAsync<StorefrontCreateCartSessionRequest, StorefrontCartSessionResponse>(
                StorefrontCartSessionRoute,
                new StorefrontCreateCartSessionRequest { CartToken = cartToken },
                "Unable to create cart right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> GetCartAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Get,
                StorefrontCartRoute,
                cartToken,
                request: null,
                "Unable to load cart right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> AddCartLineAsync(
            string cartToken,
            StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Post,
                StorefrontCartLinesRoute,
                cartToken,
                request,
                "Unable to add this item to cart right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> UpdateCartLineAsync(
            string cartToken,
            Guid lineId,
            StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Put,
                $"{StorefrontCartLinesRoute}/{lineId:D}",
                cartToken,
                request,
                "Unable to update this cart line right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> RemoveCartLineAsync(
            string cartToken,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Delete,
                $"{StorefrontCartLinesRoute}/{lineId:D}",
                cartToken,
                request: null,
                "Unable to remove this cart line right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> ClearCartAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Delete,
                StorefrontCartRoute,
                cartToken,
                request: null,
                "Unable to clear cart right now.",
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

        private async Task<StorefrontSubmitResult<TData>> SendCartAsync<TData>(
            HttpMethod method,
            string route,
            string cartToken,
            object? request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                return StorefrontSubmitResult<TData>.Failed("Cart token is required.");
            }

            try
            {
                using var message = new HttpRequestMessage(method, route);
                message.Headers.TryAddWithoutValidation(CartTokenHeaderName, cartToken);
                if (request is not null)
                {
                    message.Content = JsonContent.Create(request, options: JsonOptions);
                }

                using var response = await _httpClient.SendAsync(message, cancellationToken);
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

        private async Task<StorefrontSubmitResult<TData>> SendConsentAsync<TData>(
            HttpMethod method,
            string route,
            string? visitorKey,
            object? request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route);
                if (!string.IsNullOrWhiteSpace(visitorKey))
                {
                    message.Headers.TryAddWithoutValidation(ConsentVisitorHeaderName, visitorKey);
                }

                if (request is not null)
                {
                    message.Content = JsonContent.Create(request, options: JsonOptions);
                }

                using var response = await _httpClient.SendAsync(message, cancellationToken);
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
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
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

    public sealed record StorefrontPublicConfiguration(
        StorefrontStoreIdentity StoreIdentity,
        StorefrontBranding Branding,
        StorefrontLocaleOptions LocaleOptions,
        StorefrontCurrencyOptions CurrencyOptions,
        StorefrontConsentConfiguration Consent,
        StorefrontMaintenanceState MaintenanceState,
        StorefrontFeatureFlags FeatureFlags,
        IReadOnlyList<StorefrontPublicPaymentMethod> PaymentMethods,
        StorefrontSeoDefaults SeoDefaults);

    public sealed record StorefrontStoreIdentity(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps);

    public sealed record StorefrontBranding(
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string? SupportEmail,
        string? SupportPhone,
        string? HtmlBodyId);

    public sealed record StorefrontLocaleOptions(
        string DefaultCulture,
        IReadOnlyList<string> SupportedCultures);

    public sealed record StorefrontCurrencyOptions(
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes);

    public sealed class StorefrontCurrencyPreferenceRequest
    {
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public sealed record StorefrontCurrencyPreferenceResponse(
        string CurrencyCode,
        string BaseCurrencyCode,
        string? RequestedCurrencyCode,
        bool RequestedCurrencySupported,
        bool CheckoutCurrencyEnabled,
        string Reason);

    public sealed record StorefrontConsentConfiguration(
        bool Enabled,
        bool BannerRequired,
        string CurrentVersion,
        string PolicyPagePath,
        IReadOnlyList<StorefrontConsentCategory> Categories,
        int VisitorCookieLifetimeDays);

    public sealed record StorefrontConsentCategory(
        string Name,
        bool Required,
        bool DefaultEnabled);

    public sealed record StorefrontConsentState(
        bool Enabled,
        bool BannerRequired,
        string ConsentVersion,
        string? ConsentKey,
        StorefrontConsentCategorySelection Categories,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    public sealed record StorefrontConsentCategorySelection(
        bool Essential,
        bool Preferences,
        bool Analytics,
        bool Marketing);

    public sealed class StorefrontConsentSaveRequest
    {
        public bool Preferences { get; set; }

        public bool Analytics { get; set; }

        public bool Marketing { get; set; }
    }

    public sealed record StorefrontMaintenanceState(
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontFeatureFlags(
        bool CustomerAccountsEnabled,
        bool CartEnabled,
        bool CheckoutEnabled,
        bool PaymentsEnabled,
        bool NewsletterEnabled,
        bool RecommendationsEnabled);

    public sealed record StorefrontPublicPaymentMethod(
        Guid Id,
        string Key,
        string Name,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes);

    public sealed record StorefrontSeoDefaults(
        string? SiteName,
        string? DefaultTitleSuffix,
        string? DefaultMetaDescription,
        string? DefaultOgImage,
        string? BaseCanonicalUrl,
        string? CompanyName,
        string? CompanyLogoUrl,
        string? CompanyPhone,
        string? CompanyEmail,
        string? CompanyAddress,
        string? FacebookUrl,
        string? InstagramUrl,
        string? XUrl);

    public sealed class StorefrontCreateCartSessionRequest
    {
        public string? CartToken { get; set; }
    }

    public sealed class StorefrontCartLineCreateRequest
    {
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public string? CurrencyCode { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public sealed class StorefrontCartLineUpdateRequest
    {
        public int Quantity { get; set; }
    }

    public sealed record StorefrontCartSessionResponse(
        Guid CartId,
        string CartToken,
        string State,
        int Version,
        DateTimeOffset ExpiresAtUtc);

    public sealed record StorefrontCartResponse(
        Guid CartId,
        string State,
        int Version,
        DateTimeOffset LastActivityAtUtc,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCartLineResponse> Lines);

    public sealed record StorefrontCartLineResponse(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        string? SelectedAttributesJson,
        string? PersonalizationHash,
        string? PersonalizationJson,
        Guid? ArtworkAssetId,
        int? ArtworkVersion,
        string? FulfillmentProviderKey,
        int Quantity,
        decimal? UnitPriceSnapshot,
        string? CurrencyCodeSnapshot);

    public sealed class StorefrontCheckoutPreviewRequest
    {
        public int ExpectedCartVersion { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string PaymentMethodKey { get; set; } = string.Empty;

        public StorefrontCheckoutPreviewShippingAddress ShippingAddress { get; set; } = new();
    }

    public sealed class StorefrontCheckoutPreviewShippingAddress
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Address1 { get; set; } = string.Empty;

        public string? Address2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string? State { get; set; }

        public string PostalCode { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
    }

    public sealed record StorefrontCheckoutPreviewResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CartVersion,
        string State,
        bool IsValid,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        string PaymentMethodKey,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed record StorefrontCheckoutLineSummaryResponse(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string CurrencyCode);

    public sealed record StorefrontCheckoutValidationIssueResponse(
        string Code,
        string Message,
        string? Field,
        Guid? LineId,
        Guid? ProductId);

    public sealed class StorefrontPlaceOrderRequest
    {
        public Guid CheckoutSessionId { get; set; }

        public int ExpectedCartVersion { get; set; }

        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public sealed record StorefrontPlaceOrderResponse(
        Guid CheckoutSessionId,
        Guid PaymentAttemptId,
        Guid? OrderId,
        string? Reference,
        string? OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        decimal TotalAmount,
        string CurrencyCode,
        string IdempotencyKey,
        DateTime CreatedOn,
        StorefrontPaymentNextActionResponse? NextAction);

    public sealed record StorefrontPaymentNextActionResponse(
        string Type,
        string? Url);

    public sealed record StorefrontPaymentAttemptResponse(
        Guid Id,
        Guid CheckoutSessionId,
        Guid? OrderId,
        string PaymentMethodKey,
        string ProviderKey,
        string State,
        decimal Amount,
        string CurrencyCode,
        string? ProviderReference,
        string? ProviderSessionId,
        StorefrontPaymentNextActionResponse? NextAction,
        string? FailureCode,
        string? FailureMessage,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
