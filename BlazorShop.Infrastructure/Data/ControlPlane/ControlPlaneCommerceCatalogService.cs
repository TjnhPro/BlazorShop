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
    using BlazorShop.Application.ControlPlane.CommerceGateway.Categories;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Content;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Currencies;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Media;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Messages;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Navigation;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Orders;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Payments;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;
    using BlazorShop.Application.ControlPlane.CommerceGateway.SecurityPrivacy;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Shipping;
    using BlazorShop.Application.ControlPlane.CommerceGateway.StoreConfiguration;
    public sealed class ControlPlaneCommerceCatalogService : IControlPlaneCommerceCatalogService
    {
        private readonly IControlPlaneSecurityPrivacyGateway securityPrivacy;
        private readonly IControlPlaneStoreConfigurationGateway storeConfiguration;
        private readonly IControlPlaneMessageGateway messages;
        private readonly IControlPlaneNavigationGateway navigation;
        private readonly IControlPlaneCurrencyGateway currencies;
        private readonly IControlPlaneMediaGateway media;
        private readonly IControlPlanePaymentGateway payments;
        private readonly IControlPlaneOrderGateway orders;
        private readonly IControlPlaneProductGateway products;
        private readonly IControlPlaneContentGateway content;
        private readonly IControlPlaneShippingGateway shipping;
        private readonly IControlPlaneCategoryGateway categories;

        public ControlPlaneCommerceCatalogService(
            IControlPlaneSecurityPrivacyGateway securityPrivacy,
            IControlPlaneStoreConfigurationGateway storeConfiguration,
            IControlPlaneMessageGateway messages,
            IControlPlaneNavigationGateway navigation,
            IControlPlaneCurrencyGateway currencies,
            IControlPlaneMediaGateway media,
            IControlPlanePaymentGateway payments,
            IControlPlaneOrderGateway orders,
            IControlPlaneProductGateway products,
            IControlPlaneContentGateway content,
            IControlPlaneShippingGateway shipping,
            IControlPlaneCategoryGateway categories)
        {
            this.securityPrivacy = securityPrivacy;
            this.storeConfiguration = storeConfiguration;
            this.messages = messages;
            this.navigation = navigation;
            this.currencies = currencies;
            this.media = media;
            this.payments = payments;
            this.orders = orders;
            this.products = products;
            this.content = content;
            this.shipping = shipping;
            this.categories = categories;
        }

                public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.products.QueryProductsAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.products.GetProductAsync(storePublicId, productId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> GetRuntimeStoreAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.storeConfiguration.GetRuntimeStoreAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> UpdateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.storeConfiguration.UpdateRuntimeStoreAsync(storePublicId, runtimeStorePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> ActivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.storeConfiguration.ActivateRuntimeStoreAsync(storePublicId, runtimeStorePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> DeactivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.storeConfiguration.DeactivateRuntimeStoreAsync(storePublicId, runtimeStorePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.products.CreateProductAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateProductAsync(storePublicId, productId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.products.ArchiveProductAsync(storePublicId, productId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CategorySeoDto>> GetCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.categories.GetCategorySeoAsync(storePublicId, categoryId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CategorySeoDto>> UpdateCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategorySeoDto request,
            CancellationToken cancellationToken = default)
        {
            return this.categories.UpdateCategorySeoAsync(storePublicId, categoryId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.products.GetProductSeoAsync(storePublicId, productId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateProductSeoAsync(storePublicId, productId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.GenerateSeoSlugAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.ValidateSeoSlugAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.products.ListSeoSlugHistoryAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UploadProductImportAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.products.ListProductImportsAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.products.GetProductImportAsync(storePublicId, jobPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.products.ListProductImportRowsAsync(storePublicId, jobPublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.products.ListVariationTemplatesAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.products.GetVariationTemplateAsync(storePublicId, templatePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.CreateVariationTemplateAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateVariationTemplateAsync(storePublicId, templatePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.CreateVariationTemplateOptionAsync(storePublicId, templatePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateVariationTemplateOptionAsync(storePublicId, templatePublicId, optionPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.CreateVariationTemplateValueAsync(storePublicId, templatePublicId, optionPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateVariationTemplateValueAsync(storePublicId, templatePublicId, optionPublicId, valuePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.content.ListStorefrontPagesAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.content.ListStorefrontPageTemplatesAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.content.GetStorefrontPageTemplateStatusAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.content.GetStorefrontPageAsync(storePublicId, pagePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.content.CreateStorefrontPageAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.content.UpdateStorefrontPageAsync(storePublicId, pagePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.content.ArchiveStorefrontPageAsync(storePublicId, pagePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.content.CreateStorefrontPageDraftFromTemplateAsync(storePublicId, pageKey, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.content.MapStorefrontPageTemplateAsync(storePublicId, pagePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.content.ClearStorefrontPageTemplateAsync(storePublicId, pagePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.content.UpdateStorefrontPageNavigationAsync(storePublicId, pagePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListNavigationMenusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.ListNavigationMenusAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> GetNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.GetNavigationMenuAsync(storePublicId, menuPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> CreateNavigationMenuAsync(
            Guid storePublicId,
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.CreateNavigationMenuAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> UpdateNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.UpdateNavigationMenuAsync(storePublicId, menuPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> CreateNavigationItemAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.CreateNavigationItemAsync(storePublicId, menuPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.UpdateNavigationItemAsync(storePublicId, itemPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> ArchiveNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.ArchiveNavigationItemAsync(storePublicId, itemPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemOrderAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.UpdateNavigationItemOrderAsync(storePublicId, menuPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreNavigationTargetOptionDto>>> ListNavigationSystemTargetsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.navigation.ListNavigationSystemTargetsAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.media.ListProductMediaAsync(storePublicId, productId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.media.ImportProductMediaAsync(storePublicId, productId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.media.UpdateProductMediaOrderAsync(storePublicId, productId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.media.SetPrimaryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.media.DeleteProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.media.RetryProductMediaAsync(storePublicId, productId, mediaPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.media.ListMediaAssetsAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> GetMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.media.GetMediaAssetAsync(storePublicId, assetPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.media.UploadMediaAssetAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.media.UpdateMediaAssetMetadataAsync(storePublicId, assetPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.media.ReplaceMediaAssetAsync(storePublicId, assetPublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.media.DeleteMediaAssetAsync(storePublicId, assetPublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.categories.ListCategoriesAsync(storePublicId, 1, 25, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.categories.GetCategoryTreeAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.categories.CreateCategoryAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.categories.UpdateCategoryAsync(storePublicId, categoryId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.categories.ArchiveCategoryAsync(storePublicId, categoryId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> GetCategoryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.categories.GetCategoryMediaAsync(storePublicId, categoryId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> SetCategoryPrimaryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.products.SetCategoryPrimaryMediaAsync(storePublicId, categoryId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> ClearCategoryPrimaryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.products.ClearCategoryPrimaryMediaAsync(storePublicId, categoryId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.products.ListVariantsAsync(storePublicId, productId, 1, 25, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            return this.products.CreateVariantAsync(storePublicId, productId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateVariantAsync(storePublicId, productId, variantId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default)
        {
            return this.products.DeleteVariantAsync(storePublicId, productId, variantId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.products.QueryInventoryAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateProductStockAsync(storePublicId, productId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.products.UpdateVariantStockAsync(storePublicId, variantId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.orders.QueryOrdersAsync(storePublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.orders.GetOrderAsync(storePublicId, orderId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.orders.UpdateOrderAdminNoteAsync(storePublicId, orderId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.orders.UpdateOrderShippingStatusAsync(storePublicId, orderId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.orders.CompleteOrderAsync(storePublicId, orderId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.orders.CancelOrderAsync(storePublicId, orderId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorePaymentMethodDto>>> ListPaymentMethodsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.payments.ListPaymentMethodsAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StorePaymentMethodDto>> UpdatePaymentMethodAsync(
            Guid storePublicId,
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.payments.UpdatePaymentMethodAsync(storePublicId, paymentMethodKey, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> GetEmailSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.GetEmailSettingsAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> UpdateEmailSettingsAsync(
            Guid storePublicId,
            UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.messages.UpdateEmailSettingsAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> RotateEmailPasswordAsync(
            Guid storePublicId,
            RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.messages.RotateEmailPasswordAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> ClearEmailPasswordAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.ClearEmailPasswordAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<SendStoreEmailTestResponse>> SendEmailTestAsync(
            Guid storePublicId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.messages.SendEmailTestAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<MessageTemplateAdminSummary>>> ListMessageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.ListMessageTemplatesAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<MessageTemplateAdminDetail>> GetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.GetMessageTemplateAsync(storePublicId, templatePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<MessageTemplateAdminDetail>> UpdateMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.messages.UpdateMessageTemplateAsync(storePublicId, templatePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<MessageTemplateAdminDetail>> ResetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.ResetMessageTemplateAsync(storePublicId, templatePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<MessageTemplatePreviewResponse>> PreviewMessageTemplateAsync(
            Guid storePublicId,
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.messages.PreviewMessageTemplateAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            Guid storePublicId,
            string? status = null,
            string? templateSystemName = null,
            int skip = 0,
            int take = 25,
            CancellationToken cancellationToken = default)
        {
            return this.messages.ListQueuedMessagesAsync(storePublicId, status, templateSystemName, 0, 25, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.GetQueuedMessageAsync(storePublicId, queuedMessagePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.RetryQueuedMessageAsync(storePublicId, queuedMessagePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.messages.CancelQueuedMessageAsync(storePublicId, queuedMessagePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.ListCurrenciesAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyDto>> UpdateCurrencyAsync(
            Guid storePublicId,
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.UpdateCurrencyAsync(storePublicId, currencyCode, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.ListExchangeRatesAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.ListExchangeRateProvidersAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.FetchExchangeRatesAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<CommerceTaskSummary>> QueueExchangeRateUpdateAsync(
            Guid storePublicId,
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.QueueExchangeRateUpdateAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> UpsertExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.UpsertExchangeRateAsync(storePublicId, targetCurrencyCode, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> DisableExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            return this.currencies.DisableExchangeRateAsync(storePublicId, targetCurrencyCode, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreSecurityPrivacySettingsDto>> GetSecurityPrivacySettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.securityPrivacy.GetSecurityPrivacySettingsAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreSecurityPrivacySettingsDto>> UpdateSecurityPrivacySettingsAsync(
            Guid storePublicId,
            UpdateStoreSecurityPrivacySettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.securityPrivacy.UpdateSecurityPrivacySettingsAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreShippingSettingsDto>> GetShippingSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.shipping.GetShippingSettingsAsync(storePublicId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<StoreShippingSettingsDto>> UpdateShippingSettingsAsync(
            Guid storePublicId,
            UpdateStoreShippingSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.shipping.UpdateShippingSettingsAsync(storePublicId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.orders.GetShipmentAsync(storePublicId, orderId, cancellationToken);
        }

                public Task<ControlPlaneCommerceCatalogResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.orders.UpsertShipmentAsync(storePublicId, orderId, request, cancellationToken);
        }

                public Task<ControlPlaneCommerceMediaResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.media.GetProductMediaPreviewAsync(storePublicId, mediaPublicId, query, cancellationToken);
        }

                public Task<ControlPlaneCommerceMediaResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.media.GetMediaAssetPreviewAsync(storePublicId, assetPublicId, canonicalFileName, query, cancellationToken);
        }
    }
}

