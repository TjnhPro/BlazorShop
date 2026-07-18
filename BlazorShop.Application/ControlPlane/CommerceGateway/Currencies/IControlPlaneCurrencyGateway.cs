namespace BlazorShop.Application.ControlPlane.CommerceGateway.Currencies
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public interface IControlPlaneCurrencyGateway
    {
        
                Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
                    Guid storePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<StoreCurrencyDto>> UpdateCurrencyAsync(
                    Guid storePublicId,
                    string currencyCode,
                    UpdateStoreCurrencyRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
                    Guid storePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
                    Guid storePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
                    Guid storePublicId,
                    FetchStoreCurrencyExchangeRatesRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<CommerceTaskSummary>> QueueExchangeRateUpdateAsync(
                    Guid storePublicId,
                    QueueStoreCurrencyExchangeRateUpdateRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> UpsertExchangeRateAsync(
                    Guid storePublicId,
                    string targetCurrencyCode,
                    UpsertStoreCurrencyExchangeRateRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> DisableExchangeRateAsync(
                    Guid storePublicId,
                    string targetCurrencyCode,
                    CancellationToken cancellationToken = default);
    }
}

