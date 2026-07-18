namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public sealed class ControlPlaneCurrencyGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Currencies.IControlPlaneCurrencyGateway
    {
        public ControlPlaneCurrencyGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreCurrencyDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/currencies",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyDto>> UpdateCurrencyAsync(
            Guid storePublicId,
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/currencies/{Uri.EscapeDataString(currencyCode)}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreCurrencyExchangeRateDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/currencies/exchange-rates",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/currencies/exchange-rate-providers",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyExchangeRateProviderFetchResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/currencies/exchange-rates/fetch",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceTaskSummary>> QueueExchangeRateUpdateAsync(
            Guid storePublicId,
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceTaskSummary>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/currencies/exchange-rates/update-tasks",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> UpsertExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyExchangeRateDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> DisableExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyExchangeRateDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}/disable",
                null,
                cancellationToken);
        }
    }
}

