namespace BlazorShop.Application.ControlPlane.Catalog
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts;

    public interface IControlPlaneCommerceCatalogService
    {
        Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> GetRuntimeStoreAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> UpdateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> ActivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> DeactivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> GetMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorePaymentMethodDto>>> ListPaymentMethodsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<StorePaymentMethodDto>> UpdatePaymentMethodAsync(
            Guid storePublicId,
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default);

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

        Task<ControlPlaneCommerceCatalogResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceCatalogResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceMediaResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneCommerceMediaResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default);
    }

    public sealed record ControlPlaneCommerceCatalogResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneCommerceCatalogFailure? Failure = null,
        int? HttpStatusCode = null);

    public sealed record ProductMediaPreviewQuery(
        int? Width = null,
        int? Height = null,
        string? Fit = null,
        string? Format = null,
        int? Version = null);

    public sealed record MediaAssetPreviewQuery(
        int? Width = null,
        int? Height = null,
        string? Fit = null,
        string? Format = null,
        long? Version = null);

    public sealed record ControlPlaneCommerceMediaResult(
        bool Success,
        string? Message = null,
        byte[]? Content = null,
        string? ContentType = null,
        ControlPlaneCommerceCatalogFailure? Failure = null,
        int? HttpStatusCode = null);

    public enum ControlPlaneCommerceCatalogFailure
    {
        Validation,
        NotFound,
        RemoteFailure
    }
}
