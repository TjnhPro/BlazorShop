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

        public Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return GetAsync<StorefrontProductFilterMetadataResponse>(
                BuildProductFilterMetadataRoute(categorySlug, searchTerm, currencyCode),
                cancellationToken,
                fallbackValue: null,
                CatalogRequestTimeout);
        }

        public Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return GetAsync<StorefrontSearchSuggestionResponse>(
                BuildSearchSuggestionsRoute(searchTerm, categorySlug, limit, currencyCode),
                cancellationToken,
                fallbackValue: null,
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

        public Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed("Product is required."));
            }

            return PostAsync<StorefrontProductSelectionPreviewRequest, StorefrontProductSelectionPreviewResponse>(
                $"{StorefrontProductsRoute}/{productId:D}/selection-preview",
                request,
                "Unable to preview this product selection right now.",
                cancellationToken);
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

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>> GetAddressCountriesAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<StorefrontAddressCountryResponse>>(
                StorefrontAddressCountriesRoute,
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.Success([]);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>> GetAddressStatesAsync(
            string? countryCode,
            CancellationToken cancellationToken = default)
        {
            var normalizedCountryCode = NormalizeCountryCode(countryCode);
            if (normalizedCountryCode is null)
            {
                return StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success([]);
            }

            var result = await GetAsync<List<StorefrontAddressStateProvinceResponse>>(
                $"{StorefrontAddressCountriesRoute}/{Uri.EscapeDataString(normalizedCountryCode)}/states",
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success([]);
        }

        public Task<StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>> GetAddressConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<StorefrontAddressFieldConfigurationResponse>(
                StorefrontAddressConfigurationRoute,
                cancellationToken,
                CatalogRequestTimeout);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> GetCustomerProfileAsync(
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<StorefrontCustomerProfileResponse>(
                HttpMethod.Get,
                StorefrontCustomerProfileRoute,
                bearerToken,
                request: null,
                "Unable to load customer profile right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> UpdateCustomerProfileAsync(
            string bearerToken,
            StorefrontCustomerProfileUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<StorefrontCustomerProfileResponse>(
                HttpMethod.Put,
                StorefrontCustomerProfileRoute,
                bearerToken,
                request,
                "Unable to update customer profile right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<IReadOnlyList<StorefrontCustomerAddressResponse>>> GetCustomerAddressesAsync(
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<IReadOnlyList<StorefrontCustomerAddressResponse>>(
                HttpMethod.Get,
                StorefrontCustomerAddressesRoute,
                bearerToken,
                request: null,
                "Unable to load saved addresses right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> CreateCustomerAddressAsync(
            string bearerToken,
            StorefrontCustomerAddressRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<StorefrontCustomerAddressResponse>(
                HttpMethod.Post,
                StorefrontCustomerAddressesRoute,
                bearerToken,
                request,
                "Unable to save this address right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> UpdateCustomerAddressAsync(
            string bearerToken,
            Guid addressId,
            StorefrontCustomerAddressRequest request,
            CancellationToken cancellationToken = default)
        {
            if (addressId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerAddressResponse>.Failed("Address is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerAddressResponse>(
                HttpMethod.Put,
                $"{StorefrontCustomerAddressesRoute}/{addressId:D}",
                bearerToken,
                request,
                "Unable to update this address right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<object>> DeleteCustomerAddressAsync(
            string bearerToken,
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            if (addressId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<object>.Failed("Address is required."));
            }

            return SendAuthorizedAsync<object>(
                HttpMethod.Delete,
                $"{StorefrontCustomerAddressesRoute}/{addressId:D}",
                bearerToken,
                request: null,
                "Unable to delete this address right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultShippingAddressAsync(
            string bearerToken,
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            return SetDefaultCustomerAddressAsync(bearerToken, addressId, "default-shipping", cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultBillingAddressAsync(
            string bearerToken,
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            return SetDefaultCustomerAddressAsync(bearerToken, addressId, "default-billing", cancellationToken);
        }

        private Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultCustomerAddressAsync(
            string bearerToken,
            Guid addressId,
            string command,
            CancellationToken cancellationToken)
        {
            if (addressId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerAddressResponse>.Failed("Address is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerAddressResponse>(
                HttpMethod.Post,
                $"{StorefrontCustomerAddressesRoute}/{addressId:D}/{command}",
                bearerToken,
                request: null,
                "Unable to update this address right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<PagedResult<StorefrontCustomerOrderListItemResponse>>> GetCustomerOrdersAsync(
            string bearerToken,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var route = string.Create(
                CultureInfo.InvariantCulture,
                $"{StorefrontCustomerOrdersRoute}?pageNumber={Math.Max(1, pageNumber)}&pageSize={Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, 100)}");
            return SendAuthorizedAsync<PagedResult<StorefrontCustomerOrderListItemResponse>>(
                HttpMethod.Get,
                route,
                bearerToken,
                request: null,
                "Unable to load orders right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderAsync(
            string bearerToken,
            string orderReference,
            CancellationToken cancellationToken = default)
        {
            var reference = NormalizeOrderReference(orderReference);
            if (reference is null)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>.Failed("Order reference is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerOrderDetailResponse>(
                HttpMethod.Get,
                $"{StorefrontCustomerOrdersRoute}/{Uri.EscapeDataString(reference)}",
                bearerToken,
                request: null,
                "Unable to load this order right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderReceiptAsync(
            string bearerToken,
            string orderReference,
            CancellationToken cancellationToken = default)
        {
            var reference = NormalizeOrderReference(orderReference);
            if (reference is null)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>.Failed("Order reference is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerOrderDetailResponse>(
                HttpMethod.Get,
                $"{StorefrontCustomerOrdersRoute}/{Uri.EscapeDataString(reference)}/receipt",
                bearerToken,
                request: null,
                "Unable to load this receipt right now.",
                cancellationToken);
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

        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> StartCheckoutAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                StorefrontCheckoutStartRoute,
                cartToken,
                new StorefrontCheckoutStartRequest(),
                "Unable to start checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> LoadCheckoutAsync(
            string cartToken,
            Guid checkoutSessionId,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Get,
                $"checkout/{checkoutSessionId:D}",
                cartToken,
                request: null,
                "Unable to load checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> UpdateCheckoutAddressesAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/addresses",
                cartToken,
                request,
                "Unable to update checkout address right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutShippingMethodAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/shipping-method",
                cartToken,
                request,
                "Unable to update shipping method right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutPaymentMethodAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/payment-method",
                cartToken,
                request,
                "Unable to update payment method right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCheckoutReviewResponse>> ReviewCheckoutAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutReviewResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutReviewResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/review",
                cartToken,
                request,
                "Unable to review checkout right now.",
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

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> RecalculateCartAsync(
            string cartToken,
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Post,
                StorefrontCartRecalculateRoute,
                cartToken,
                request,
                "Unable to refresh cart right now.",
                cancellationToken);
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> MergeCurrentCustomerCartAsync(
            string cartToken,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Post,
                StorefrontCartMergeCurrentCustomerRoute,
                cartToken,
                request: null,
                "Unable to merge cart right now.",
                cancellationToken,
                accessToken);
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
            CancellationToken cancellationToken,
            string? bearerToken = null)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                return StorefrontSubmitResult<TData>.Failed("Cart token is required.");
            }

            try
            {
                using var message = new HttpRequestMessage(method, route);
                message.Headers.TryAddWithoutValidation(CartTokenHeaderName, cartToken);
                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
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

        private async Task<StorefrontSubmitResult<TData>> SendAuthorizedAsync<TData>(
            HttpMethod method,
            string route,
            string bearerToken,
            object? request,
            string unavailableMessage,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                return StorefrontSubmitResult<TData>.Failed("Customer identity is required.");
            }

            try
            {
                using var message = new HttpRequestMessage(method, route);
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

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

    public sealed record StorefrontCustomerProfileResponse(
        Guid CustomerPublicId,
        string Email,
        string FullName,
        string? FirstName,
        string? LastName,
        string? Company,
        string? PhoneNumber,
        string? PreferredLanguage,
        string? PreferredCurrencyCode,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? LastActivityAtUtc);

    public sealed class StorefrontCustomerProfileUpdateRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Company { get; set; }

        public string? PhoneNumber { get; set; }

        public string? PreferredLanguage { get; set; }

        public string? PreferredCurrencyCode { get; set; }
    }

    public sealed record StorefrontProductFilterMetadataResponse(
        IReadOnlyList<int> PageSizes,
        IReadOnlyList<StorefrontProductSortOptionResponse> SortOptions,
        IReadOnlyList<StorefrontFilterFacetResponse> Facets,
        StorefrontPriceFacetResponse PriceRange,
        int MinimumSearchTermLength);

    public sealed record StorefrontFilterFacetResponse(
        string Key,
        string Label,
        string Type,
        int DisplayOrder,
        int? MaxChoices,
        int MinimumHitCount,
        IReadOnlyList<StorefrontFilterChoiceResponse> Choices);

    public sealed record StorefrontFilterChoiceResponse(
        string Value,
        string Label,
        int DisplayOrder,
        int? HitCount,
        bool Selected);

    public sealed record StorefrontPriceFacetResponse(
        decimal? MinPrice,
        decimal? MaxPrice,
        string? CurrencyCode,
        int DisplayOrder);

    public sealed record StorefrontProductSortOptionResponse(
        string Value,
        string Label,
        int DisplayOrder);

    public sealed record StorefrontSearchSuggestionResponse(
        string? SearchTerm,
        int MinimumSearchTermLength,
        int Limit,
        IReadOnlyList<StorefrontSearchSuggestionItemResponse> Items);

    public sealed record StorefrontSearchSuggestionItemResponse(
        Guid Id,
        string Slug,
        string Name,
        string? Sku,
        string? Image,
        Guid? PrimaryMediaPublicId,
        bool HasPrimaryMedia,
        decimal Price,
        decimal? DisplayPrice,
        string? DisplayCurrencyCode,
        string? CategoryName,
        string? CategorySlug,
        bool InStock,
        string Url);

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
        StorefrontCaptchaConfiguration Captcha,
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

    public sealed class StorefrontProductSelectionPreviewRequest
    {
        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        public int Quantity { get; set; } = 1;

        public string? CurrencyCode { get; set; }
    }

    public sealed record StorefrontProductSelectionPreviewResponse(
        Guid ProductId,
        Guid? ProductVariantId,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        IReadOnlyList<StorefrontProductSelectionAttribute> SelectedAttributes,
        string? AttributeSignature,
        string? Sku,
        string? DisplayName,
        decimal UnitPrice,
        decimal? ComparePrice,
        string CurrencyCode,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity,
        string? PrimaryImageUrl);

    public sealed record StorefrontProductSelectionAttribute(string Name, string Value);

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

    public sealed record StorefrontCaptchaConfiguration(
        bool Enabled,
        string ProviderSystemName,
        string? PublicSiteKey,
        IReadOnlyList<string> EnabledTargets,
        IReadOnlyDictionary<string, string> ActionNames);

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

    public sealed class StorefrontCartRecalculateRequest
    {
        public int? ExpectedVersion { get; set; }
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
        IReadOnlyList<StorefrontCartLineResponse> Lines,
        string CurrencyCode = "USD",
        int SummaryCount = 0,
        decimal Subtotal = 0m,
        decimal DiscountTotal = 0m,
        decimal ShippingEstimate = 0m,
        decimal TaxEstimate = 0m,
        decimal GrandTotal = 0m,
        bool CheckoutAllowed = true,
        IReadOnlyList<StorefrontCartWarningResponse>? Warnings = null,
        IReadOnlyList<StorefrontCartAdjustmentResponse>? Adjustments = null);

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
        string? CurrencyCodeSnapshot,
        string? DisplayName = null,
        string? ProductSlug = null,
        string? ProductUrl = null,
        string? ImageUrl = null,
        IReadOnlyList<StorefrontCartSelectedAttributeResponse>? SelectedAttributes = null,
        decimal? UnitPrice = null,
        decimal? LineSubtotal = null,
        decimal? LineTotal = null,
        int QuantityMinimum = 1,
        int? QuantityMaximum = null,
        int QuantityStep = 1,
        IReadOnlyList<int>? AllowedQuantities = null,
        bool Purchasable = true,
        IReadOnlyList<StorefrontCartWarningResponse>? Warnings = null);

    public sealed record StorefrontCartSelectedAttributeResponse(
        string Name,
        string Value);

    public sealed record StorefrontCartWarningResponse(
        string Code,
        string Message,
        Guid? LineId,
        Guid? ProductId);

    public sealed record StorefrontCartAdjustmentResponse(
        string Code,
        string Label,
        decimal Amount,
        string CurrencyCode);

    public sealed class StorefrontCheckoutPreviewRequest
    {
        public int ExpectedCartVersion { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string PaymentMethodKey { get; set; } = string.Empty;

        public Guid? ShippingAddressId { get; set; }

        public Guid? BillingAddressId { get; set; }

        public bool UseShippingAddressAsBillingAddress { get; set; } = true;

        public StorefrontCheckoutPreviewShippingAddress? ShippingAddress { get; set; } = new();
    }

    public sealed class StorefrontCheckoutStartRequest
    {
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

    public sealed class StorefrontCheckoutAddressStepRequest
    {
        public Guid? BillingAddressId { get; set; }

        public Guid? ShippingAddressId { get; set; }

        public bool UseBillingAddressAsShippingAddress { get; set; }

        public StorefrontCheckoutPreviewShippingAddress? BillingAddress { get; set; }

        public StorefrontCheckoutPreviewShippingAddress? ShippingAddress { get; set; }
    }

    public sealed class StorefrontCheckoutShippingMethodRequest
    {
        public string ShippingOptionKey { get; set; } = string.Empty;
    }

    public sealed class StorefrontCheckoutPaymentMethodRequest
    {
        public string PaymentMethodKey { get; set; } = string.Empty;
    }

    public sealed class StorefrontCheckoutReviewRequest
    {
        public bool TermsAccepted { get; set; }

        public string? TermsVersion { get; set; }
    }

    public sealed record StorefrontCheckoutShippingOptionResponse(
        string Key,
        string DisplayName,
        string? Description,
        decimal Price,
        string CurrencyCode,
        string? DeliveryEstimateText,
        bool Selected);

    public sealed record StorefrontCheckoutPaymentMethodOptionResponse(
        string Key,
        string DisplayName,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        string ProviderKey,
        string NextActionKind,
        bool Selected);

    public sealed record StorefrontCheckoutSessionResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
        bool IsActive,
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
        bool ShippingRequired,
        StorefrontCheckoutShippingOptionResponse? SelectedShippingOption,
        IReadOnlyList<StorefrontCheckoutShippingOptionResponse> ShippingOptions,
        StorefrontCheckoutPaymentMethodOptionResponse? SelectedPaymentMethod,
        IReadOnlyList<StorefrontCheckoutPaymentMethodOptionResponse> PaymentMethods,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed record StorefrontCheckoutReviewResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
        bool IsActive,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        StorefrontCheckoutPreviewShippingAddress? BillingAddress,
        StorefrontCheckoutPreviewShippingAddress? ShippingAddress,
        StorefrontCheckoutShippingOptionResponse? SelectedShippingOption,
        StorefrontCheckoutPaymentMethodOptionResponse? SelectedPaymentMethod,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        bool TermsRequired,
        bool TermsAccepted,
        string? TermsVersion,
        DateTimeOffset? TermsAcceptedAtUtc,
        bool PlaceOrderAllowed,
        string NextRequiredStep,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed class StorefrontPlaceOrderRequest
    {
        public Guid CheckoutSessionId { get; set; }

        public int ExpectedCheckoutVersion { get; set; }

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
        string? GuestAccessToken,
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

    public sealed record StorefrontAddressCountryResponse(
        string Code,
        string Name,
        bool PostalCodeRequired,
        bool StateProvinceRequired);

    public sealed record StorefrontAddressStateProvinceResponse(
        string Code,
        string Name);

    public sealed record StorefrontAddressFieldConfigurationResponse(
        bool CompanyEnabled,
        bool PhoneEnabled,
        bool PhoneRequired,
        bool PostalCodeRequired,
        bool BillingAddressEnabled,
        bool UseShippingAddressAsBillingDefault,
        int FirstNameMaxLength,
        int LastNameMaxLength,
        int CompanyMaxLength,
        int AddressLineMaxLength,
        int CityMaxLength,
        int PostalCodeMaxLength,
        int StateProvinceCodeMaxLength,
        int StateProvinceNameMaxLength,
        int PhoneMaxLength,
        int EmailMaxLength,
        IReadOnlyList<string> StateProvinceRequiredCountryCodes);

    public sealed class StorefrontCustomerAddressRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Company { get; set; }

        public string Address1 { get; set; } = string.Empty;

        public string? Address2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;

        public string? StateProvinceCode { get; set; }

        public string? StateProvinceName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public bool IsDefaultShipping { get; set; }

        public bool IsDefaultBilling { get; set; }
    }

    public sealed record StorefrontCustomerAddressResponse(
        Guid PublicId,
        string FirstName,
        string LastName,
        string? Company,
        string Address1,
        string? Address2,
        string City,
        string PostalCode,
        string CountryCode,
        string? StateProvinceCode,
        string? StateProvinceName,
        string? Phone,
        string? Email,
        bool IsDefaultShipping,
        bool IsDefaultBilling,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public sealed record StorefrontCustomerOrderListItemResponse(
        string Reference,
        DateTime CreatedOn,
        string OrderStatus,
        string PaymentStatus,
        string ShippingStatus,
        string? CurrencyCode,
        decimal TotalAmount,
        int ItemCount,
        StorefrontCustomerOrderTrackingSummaryResponse TrackingSummary);

    public sealed record StorefrontCustomerOrderTrackingSummaryResponse(
        string? ShippingCarrier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedOn,
        DateTime? DeliveredOn,
        DateTimeOffset? LastTrackingEventAtUtc);

    public sealed record StorefrontCustomerOrderDetailResponse(
        string Reference,
        string Status,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime? PaymentAt,
        StorefrontOrderPaymentSummaryResponse? PaymentSummary,
        StorefrontOrderStoreSnapshotResponse? StoreSnapshot,
        string? CurrencyCode,
        decimal TotalAmount,
        StorefrontOrderTotalBreakdownResponse? TotalBreakdown,
        string? BaseCurrencyCode,
        decimal? BaseTotalAmount,
        StorefrontOrderTotalBreakdownResponse? BaseTotalBreakdown,
        decimal? ExchangeRate,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc,
        DateTime CreatedOn,
        string ShippingStatus,
        string? ShippingCarrier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedOn,
        DateTime? DeliveredOn,
        string? CustomerName,
        string? CustomerEmail,
        StorefrontShippingAddressResponse? BillingAddress,
        StorefrontShippingAddressResponse? ShippingAddressSnapshot,
        StorefrontShippingAddressResponse ShippingAddress,
        StorefrontCustomerOrderShippingMethodResponse? ShippingMethod,
        DateTime? CompletedAt,
        DateTime? CancelledAt,
        IReadOnlyList<StorefrontOrderTrackingEventResponse> TrackingEvents,
        IReadOnlyList<StorefrontOrderHistoryEntryResponse> HistoryEntries,
        IReadOnlyList<StorefrontOrderLineResponse> Lines,
        StorefrontCustomerOrderActionFlagsResponse Actions,
        bool ReceiptMode);

    public sealed record StorefrontOrderPaymentSummaryResponse(
        string PaymentStatus,
        string PaymentMethodKey,
        string? AttemptState,
        decimal? Amount,
        string? CurrencyCode,
        DateTime? PaymentAt,
        DateTimeOffset? UpdatedAtUtc);

    public sealed record StorefrontOrderStoreSnapshotResponse(
        Guid? PublicId,
        string? StoreKey,
        string? Name,
        string? BaseUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress);

    public sealed record StorefrontOrderTotalBreakdownResponse(
        decimal? Subtotal,
        decimal? ShippingTotal,
        decimal? TaxTotal,
        decimal? DiscountTotal,
        decimal? GrandTotal);

    public sealed record StorefrontCustomerOrderShippingMethodResponse(
        string? Key,
        string? MethodCode,
        string? Name,
        decimal? Total,
        string? CurrencyCode,
        string? DeliveryEstimateText);

    public sealed record StorefrontOrderTrackingEventResponse(
        string Status,
        string Message,
        DateTime OccurredAtUtc,
        string? Location,
        string Source);

    public sealed record StorefrontOrderHistoryEntryResponse(
        string EventType,
        string? OldValue,
        string? NewValue,
        string Message,
        DateTimeOffset CreatedAtUtc);

    public sealed record StorefrontShippingAddressResponse(
        string? FullName,
        string? Email,
        string? Phone,
        string? Address1,
        string? Address2,
        string? City,
        string? State,
        string? PostalCode,
        string? CountryCode);

    public sealed record StorefrontOrderLineResponse(
        Guid ProductId,
        string? ProductName,
        string? Sku,
        string? Image,
        Guid? ProductVariantId,
        IReadOnlyList<SelectedAttributeDto> VariantAttributes,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public sealed record StorefrontCustomerOrderActionFlagsResponse(
        bool CanRetryPayment,
        bool CanReorder,
        bool CanRequestReturn,
        bool HasDownloads);
}
