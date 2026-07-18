namespace BlazorShop.ControlPlane.Web.Services.Catalog
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

    public interface IControlPlaneCatalogClient
    {
        Task<ControlPlaneClientResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CategorySeoDto>> GetCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CategorySeoDto>> UpdateCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategorySeoDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneFileResult> DownloadProductImportTemplateAsync(
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? mode,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneFileResult> DownloadProductImportErrorsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneFileResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            Stream content,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneFileResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListNavigationMenusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> GetNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> CreateNavigationMenuAsync(
            Guid storePublicId,
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> CreateNavigationItemAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> ArchiveNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemOrderAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StoreNavigationTargetOptionDto>>> ListNavigationSystemTargetsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StorePaymentMethodDto>>> ListPaymentMethodsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StorePaymentMethodDto>> UpdatePaymentMethodAsync(
            Guid storePublicId,
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> GetEmailSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> UpdateEmailSettingsAsync(
            Guid storePublicId,
            UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> RotateEmailPasswordAsync(
            Guid storePublicId,
            RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> ClearEmailPasswordAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<SendStoreEmailTestResponse>> SendEmailTestAsync(
            Guid storePublicId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<MessageTemplateAdminSummary>>> ListMessageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> GetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> UpdateMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> ResetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplatePreviewResponse>> PreviewMessageTemplateAsync(
            Guid storePublicId,
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            Guid storePublicId,
            string? status = null,
            string? templateSystemName = null,
            int skip = 0,
            int take = 25,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreCurrencyDto>> UpdateCurrencyAsync(
            Guid storePublicId,
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<CommerceTaskSummary>> QueueExchangeRateUpdateAsync(
            Guid storePublicId,
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreCurrencyExchangeRateDto>> UpsertExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreCurrencyExchangeRateDto>> DisableExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneCatalogClient : IControlPlaneCatalogClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneCatalogClient(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public Task<ControlPlaneClientResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetCatalogProduct>>(
                CommerceRoute(storePublicId, "products") + BuildProductQuery(query),
                "Unable to load catalog products.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetProduct>(
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                "Unable to load product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateProduct, object>(
                CommerceRoute(storePublicId, "products"),
                request,
                "Unable to create product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProduct, object>(
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                request,
                "Unable to update product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                "Unable to archive product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CategorySeoDto>> GetCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<CategorySeoDto>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}/seo"),
                "Unable to load category SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CategorySeoDto>> UpdateCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategorySeoDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateCategorySeoDto, CategorySeoDto>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}/seo"),
                request,
                "Unable to update category SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductSeoDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/seo"),
                "Unable to load product SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductSeoDto, ProductSeoDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/seo"),
                request,
                "Unable to update product SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<StoreSeoSlugGenerateRequest, StoreSeoSlugPolicyResult>(
                CommerceRoute(storePublicId, "seo/slugs/generate"),
                request,
                "Unable to generate SEO slug.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<StoreSeoSlugValidateRequest, StoreSeoSlugPolicyResult>(
                CommerceRoute(storePublicId, "seo/slugs/validate"),
                request,
                "Unable to validate SEO slug.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StoreSeoSlugHistoryDto>>(
                CommerceRoute(storePublicId, "seo/slugs/history") + BuildSeoSlugHistoryQuery(query),
                "Unable to load SEO slug history.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> DownloadProductImportTemplateAsync(
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                "api/controlplane/commerce/product-imports/template",
                "Unable to download product import template.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? mode,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "products.csv" : fileName);
            form.Add(new StringContent(string.IsNullOrWhiteSpace(mode) ? ProductImportModes.CreateOnly : mode), "mode");

            return await this.apiClient.PostPrivateMultipartAsync<ProductImportUploadResponse>(
                CommerceRoute(storePublicId, "product-imports"),
                form,
                "Unable to upload product import.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductImportJobListResponse>(
                CommerceRoute(storePublicId, "product-imports") + BuildProductImportQuery(query),
                "Unable to load product import jobs.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductImportJobDetailDto>(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}"),
                "Unable to load product import job.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductImportRowsResponse>(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}/rows") + BuildProductImportRowsQuery(query),
                "Unable to load product import rows.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> DownloadProductImportErrorsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}/errors.csv"),
                "Unable to download product import errors.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media") + BuildPageQuery(query.PageNumber, query.PageSize),
                "Unable to load product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<ImportProductMediaRequest, ImportProductMediaResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/import"),
                request,
                "Unable to import product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductMediaOrderRequest, ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/order"),
                request,
                "Unable to update product media order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<ProductMediaDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/primary"),
                "Unable to set primary product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}"),
                "Unable to delete product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<ImportProductMediaResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/retry"),
                "Unable to retry product media.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/preview") + BuildMediaPreviewQuery(query),
                "Unable to load product media preview.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<CommerceMediaAssetListResponse>(
                CommerceRoute(storePublicId, "media/assets") + BuildMediaAssetListQuery(query),
                "Unable to load media assets.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            using var form = BuildMediaAssetForm(content, fileName, contentType);
            return await this.apiClient.PostPrivateMultipartAsync<CommerceMediaAssetDto>(
                CommerceRoute(storePublicId, "media/assets"),
                form,
                "Unable to upload media asset.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<CommerceMediaAssetMetadataRequest, CommerceMediaAssetDto>(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}"),
                request,
                "Unable to update media asset.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            Stream content,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            using var form = BuildMediaAssetForm(content, fileName, contentType);
            return await this.apiClient.PostPrivateMultipartAsync<CommerceMediaAssetDto>(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}/replace"),
                form,
                "Unable to replace media asset.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}"),
                "Unable to delete media asset.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}/preview") + BuildMediaAssetPreviewQuery(canonicalFileName, query),
                "Unable to load media asset preview.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetCategory>>(
                CommerceRoute(storePublicId, "categories") + BuildPageQuery(pageNumber, pageSize),
                "Unable to load categories.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<GetCategoryTreeNode>>(
                CommerceRoute(storePublicId, "categories/tree"),
                "Unable to load category tree.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateCategory, object>(
                CommerceRoute(storePublicId, "categories"),
                request,
                "Unable to create category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateCategory, object>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}"),
                request,
                "Unable to update category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}"),
                "Unable to archive category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetProductVariant>>(
                CommerceRoute(storePublicId, $"products/{productId:D}/variants") + BuildPageQuery(pageNumber, pageSize),
                "Unable to load variants.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateProductVariant, object>(
                CommerceRoute(storePublicId, $"products/{productId:D}/variants"),
                request,
                "Unable to create variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductVariant, object>(
                CommerceRoute(storePublicId, $"products/{productId:D}/variants/{variantId:D}"),
                request,
                "Unable to update variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"products/{productId:D}/variants/{variantId:D}"),
                "Unable to delete variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<AdminInventoryItemDto>>(
                CommerceRoute(storePublicId, "inventory") + BuildInventoryQuery(query),
                "Unable to load inventory.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductStockDto, AdminInventoryItemDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/inventory"),
                request,
                "Unable to update product stock.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariantStockDto, AdminInventoryVariantDto>(
                CommerceRoute(storePublicId, $"inventory/variants/{variantId:D}"),
                request,
                "Unable to update variant stock.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<VariationTemplateListResponse>(
                CommerceRoute(storePublicId, "variation-templates") + BuildPageQuery(query.PageNumber, query.PageSize),
                "Unable to load variation templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}"),
                "Unable to load variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateVariationTemplateRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, "variation-templates"),
                request,
                "Unable to create variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariationTemplateRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}"),
                request,
                "Unable to update variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateVariationTemplateOptionRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options"),
                request,
                "Unable to create variation option.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariationTemplateOptionRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}"),
                request,
                "Unable to update variation option.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateVariationTemplateValueRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values"),
                request,
                "Unable to create variation value.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariationTemplateValueRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values/{valuePublicId:D}"),
                request,
                "Unable to update variation value.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<StorefrontPageListResponse>(
                CommerceRoute(storePublicId, "pages") + BuildStorefrontPageQuery(query),
                "Unable to load storefront pages.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>(
                CommerceRoute(storePublicId, "pages/templates"),
                "Unable to load storefront page templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StorefrontPageTemplateStatusDto>>(
                CommerceRoute(storePublicId, "pages/template-status"),
                "Unable to load storefront page template status.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}"),
                "Unable to load storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateStorefrontPageRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, "pages"),
                request,
                "Unable to create storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateStorefrontPageRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}"),
                request,
                "Unable to update storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}"),
                "Unable to archive storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreatePageFromTemplateRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/templates/{Uri.EscapeDataString(pageKey)}/draft"),
                request,
                "Unable to create storefront page draft.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<MapPageTemplateRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}/template"),
                request,
                "Unable to map storefront page template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}/template"),
                "Unable to clear storefront page template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdatePageNavigationRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}/navigation"),
                request,
                "Unable to update storefront page navigation.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListNavigationMenusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StoreNavigationMenuSummaryDto>>(
                CommerceRoute(storePublicId, "navigation/menus"),
                "Unable to load navigation menus.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> GetNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}"),
                "Unable to load navigation menu.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> CreateNavigationMenuAsync(
            Guid storePublicId,
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateStoreNavigationMenuRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, "navigation/menus"),
                request,
                "Unable to create navigation menu.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateStoreNavigationMenuRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}"),
                request,
                "Unable to update navigation menu.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> CreateNavigationItemAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateStoreNavigationMenuItemRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}/items"),
                request,
                "Unable to create navigation item.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateStoreNavigationMenuItemRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/items/{itemPublicId:D}"),
                request,
                "Unable to update navigation item.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> ArchiveNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/items/{itemPublicId:D}"),
                "Unable to archive navigation item.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemOrderAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateStoreNavigationMenuItemOrderRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}/items/order"),
                request,
                "Unable to update navigation item order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreNavigationTargetOptionDto>>> ListNavigationSystemTargetsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StoreNavigationTargetOptionDto>>(
                CommerceRoute(storePublicId, "navigation/system-targets"),
                "Unable to load navigation system targets.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetOrder>>(
                CommerceRoute(storePublicId, "orders") + BuildOrderQuery(query),
                "Unable to load orders.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}"),
                "Unable to load order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateOrderAdminNoteRequest, GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/admin-note"),
                request,
                "Unable to update order note.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateShippingStatusRequest, GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipping-status"),
                request,
                "Unable to update shipping status.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/complete"),
                "Unable to complete order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/cancel"),
                "Unable to cancel order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StorePaymentMethodDto>>> ListPaymentMethodsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StorePaymentMethodDto>>(
                CommerceRoute(storePublicId, "payment-methods"),
                "Unable to load payment methods.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorePaymentMethodDto>> UpdatePaymentMethodAsync(
            Guid storePublicId,
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateStorePaymentMethodRequest, StorePaymentMethodDto>(
                CommerceRoute(storePublicId, $"payment-methods/{Uri.EscapeDataString(paymentMethodKey)}"),
                request,
                "Unable to update payment method.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> GetEmailSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings"),
                "Unable to load store email settings.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> UpdateEmailSettingsAsync(
            Guid storePublicId,
            UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateStoreEmailSettingsRequest, StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings"),
                request,
                "Unable to update store email settings.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> RotateEmailPasswordAsync(
            Guid storePublicId,
            RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<RotateStoreEmailPasswordRequest, StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings/password/rotate"),
                request,
                "Unable to rotate store SMTP password.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> ClearEmailPasswordAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings/password/clear"),
                "Unable to clear store SMTP password.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<SendStoreEmailTestResponse>> SendEmailTestAsync(
            Guid storePublicId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<SendStoreEmailTestRequest, SendStoreEmailTestResponse>(
                CommerceRoute(storePublicId, "email-settings/test-send"),
                request,
                "Unable to send store SMTP test email.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<MessageTemplateAdminSummary>>> ListMessageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<MessageTemplateAdminSummary>>(
                CommerceRoute(storePublicId, "message-templates"),
                "Unable to load message templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> GetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<MessageTemplateAdminDetail>(
                CommerceRoute(storePublicId, $"message-templates/{templatePublicId:D}"),
                "Unable to load message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> UpdateMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateMessageTemplateRequest, MessageTemplateAdminDetail>(
                CommerceRoute(storePublicId, $"message-templates/{templatePublicId:D}"),
                request,
                "Unable to update message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> ResetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<MessageTemplateAdminDetail>(
                CommerceRoute(storePublicId, $"message-templates/{templatePublicId:D}/reset"),
                "Unable to reset message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplatePreviewResponse>> PreviewMessageTemplateAsync(
            Guid storePublicId,
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<PreviewMessageTemplateRequest, MessageTemplatePreviewResponse>(
                CommerceRoute(storePublicId, "message-templates/preview"),
                request,
                "Unable to preview message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            Guid storePublicId,
            string? status = null,
            string? templateSystemName = null,
            int skip = 0,
            int take = 25,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<QueuedMessageAdminListResponse>(
                CommerceRoute(storePublicId, "queued-messages" + BuildQueuedMessageQuery(status, templateSystemName, skip, take)),
                "Unable to load queued messages.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<QueuedMessageAdminDetail>(
                CommerceRoute(storePublicId, $"queued-messages/{queuedMessagePublicId:D}"),
                "Unable to load queued message.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<QueuedMessageAdminDetail>(
                CommerceRoute(storePublicId, $"queued-messages/{queuedMessagePublicId:D}/retry"),
                "Unable to retry queued message.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<QueuedMessageAdminDetail>(
                CommerceRoute(storePublicId, $"queued-messages/{queuedMessagePublicId:D}/cancel"),
                "Unable to cancel queued message.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StoreCurrencyDto>>(
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
            return this.apiClient.PutPrivateAsync<UpdateStoreCurrencyRequest, StoreCurrencyDto>(
                CommerceRoute(storePublicId, $"currencies/{Uri.EscapeDataString(currencyCode)}"),
                request,
                "Unable to update store currency.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StoreCurrencyExchangeRateDto>>(
                CommerceRoute(storePublicId, "currencies/exchange-rates"),
                "Unable to load exchange rates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>(
                CommerceRoute(storePublicId, "currencies/exchange-rate-providers"),
                "Unable to load exchange-rate providers.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<FetchStoreCurrencyExchangeRatesRequest, StoreCurrencyExchangeRateProviderFetchResult>(
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
            return this.apiClient.PostPrivateAsync<QueueStoreCurrencyExchangeRateUpdateRequest, CommerceTaskSummary>(
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
            return this.apiClient.PutPrivateAsync<UpsertStoreCurrencyExchangeRateRequest, StoreCurrencyExchangeRateDto>(
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
            return this.apiClient.PostPrivateAsync<StoreCurrencyExchangeRateDto>(
                CommerceRoute(storePublicId, $"currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}/disable"),
                "Unable to disable exchange rate.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetShipment>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipment"),
                "Unable to load shipment.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpsertShipmentRequest, GetShipment>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipment"),
                request,
                "Unable to save shipment.",
                cancellationToken);
        }

        private static string BuildProductQuery(ProductCatalogQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", query.GetNormalizedPageNumber().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("pageSize", query.GetNormalizedPageSize().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("sortBy", query.SortBy.ToString()),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "categoryId", query.CategoryId?.ToString("D"));
            AddIfPresent(values, "minPrice", query.MinPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(values, "maxPrice", query.MaxPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(values, "inStock", query.InStock?.ToString());
            AddIfPresent(values, "isPublished", query.IsPublished?.ToString());
            return ToQueryString(values);
        }

        private static string BuildInventoryQuery(AdminInventoryQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("lowStockOnly", query.LowStockOnly.ToString()),
                new("outOfStockOnly", query.OutOfStockOnly.ToString()),
                new("lowStockThreshold", Math.Max(0, query.LowStockThreshold).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            return ToQueryString(values);
        }

        private static string BuildProductImportQuery(ProductImportJobListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildPageQuery(int pageNumber, int pageSize)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, pageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(pageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            return ToQueryString(values);
        }

        private static string BuildProductImportRowsQuery(ProductImportRowsQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildStorefrontPageQuery(StorefrontPageListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "search", query.Search);
            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildSeoSlugHistoryQuery(StoreSeoSlugHistoryQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "entityType", query.EntityType);
            if (query.EntityId != Guid.Empty)
            {
                values.Add(new KeyValuePair<string, string>("entityId", query.EntityId.ToString("D")));
            }

            AddIfPresent(values, "languageCode", query.LanguageCode);
            return ToQueryString(values);
        }

        private static string BuildOrderQuery(AdminOrderQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "status", query.Status);
            AddIfPresent(values, "shippingStatus", query.ShippingStatus);
            AddIfPresent(values, "fromUtc", query.FromUtc?.ToString("O", CultureInfo.InvariantCulture));
            AddIfPresent(values, "toUtc", query.ToUtc?.ToString("O", CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string BuildQueuedMessageQuery(string? status, string? templateSystemName, int skip, int take)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("skip", Math.Max(0, skip).ToString(CultureInfo.InvariantCulture)),
                new("take", Math.Clamp(take, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", status);
            AddIfPresent(values, "templateSystemName", templateSystemName);
            return ToQueryString(values);
        }

        private static string BuildMediaPreviewQuery(ProductMediaPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string BuildMediaAssetPreviewQuery(string canonicalFileName, MediaAssetPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("fileName", canonicalFileName),
            };

            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string BuildMediaAssetListQuery(CommerceMediaAssetListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "search", query.Search);
            return ToQueryString(values);
        }

        private static MultipartFormDataContent BuildMediaAssetForm(Stream content, string fileName, string? contentType)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "media-asset" : fileName);
            return form;
        }

        private static string CommerceRoute(Guid storePublicId, string path)
        {
            return $"api/controlplane/commerce/stores/{storePublicId:D}/{path.TrimStart('/')}";
        }

        private static void AddIfPresent(List<KeyValuePair<string, string>> values, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(new KeyValuePair<string, string>(key, value.Trim()));
            }
        }

        private static string ToQueryString(IReadOnlyCollection<KeyValuePair<string, string>> values)
        {
            return values.Count == 0
                ? string.Empty
                : "?" + string.Join(
                    "&",
                    values.Select(value => Uri.EscapeDataString(value.Key) + "=" + Uri.EscapeDataString(value.Value)));
        }
    }
}
