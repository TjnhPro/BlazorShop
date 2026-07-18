namespace BlazorShop.ControlPlane.Web.Services.Commerce
{
    using System.Globalization;
    using System.Net.Http.Headers;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Domain.Contracts;

        public sealed class ControlPlaneCurrencyClient : ControlPlaneCommerceClientBase, IControlPlaneCurrencyClient
    {
        public ControlPlaneCurrencyClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StoreCurrencyDto>>(
                CommerceRoute(storePublicId, "currencies"),
                "Unable to load store currencies.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreCurrencyDto>> UpdateCurrencyAsync(
            Guid storePublicId,
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStoreCurrencyRequest, StoreCurrencyDto>(
                CommerceRoute(storePublicId, $"currencies/{Uri.EscapeDataString(currencyCode)}"),
                request,
                "Unable to update store currency.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StoreCurrencyExchangeRateDto>>(
                CommerceRoute(storePublicId, "currencies/exchange-rates"),
                "Unable to load exchange rates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>(
                CommerceRoute(storePublicId, "currencies/exchange-rate-providers"),
                "Unable to load exchange-rate providers.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<FetchStoreCurrencyExchangeRatesRequest, StoreCurrencyExchangeRateProviderFetchResult>(
                CommerceRoute(storePublicId, "currencies/exchange-rates/fetch"),
                request,
                "Unable to fetch exchange rates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CommerceTaskSummary>> QueueExchangeRateUpdateAsync(
            Guid storePublicId,
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<QueueStoreCurrencyExchangeRateUpdateRequest, CommerceTaskSummary>(
                CommerceRoute(storePublicId, "currencies/exchange-rates/update-tasks"),
                request,
                "Unable to queue exchange-rate update.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreCurrencyExchangeRateDto>> UpsertExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpsertStoreCurrencyExchangeRateRequest, StoreCurrencyExchangeRateDto>(
                CommerceRoute(storePublicId, $"currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}"),
                request,
                "Unable to save exchange rate.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreCurrencyExchangeRateDto>> DisableExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<StoreCurrencyExchangeRateDto>(
                CommerceRoute(storePublicId, $"currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}/disable"),
                "Unable to disable exchange rate.",
                cancellationToken);
        }
    }
}

