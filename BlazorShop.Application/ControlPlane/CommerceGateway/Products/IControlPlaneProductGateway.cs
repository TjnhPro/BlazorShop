namespace BlazorShop.Application.ControlPlane.CommerceGateway.Products
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
    public interface IControlPlaneProductGateway
    {
        
                Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
                    Guid storePublicId,
                    ProductCatalogQuery query,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<GetProduct>> GetProductAsync(
                    Guid storePublicId,
                    Guid productId,
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

        
                Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
                    Guid storePublicId,
                    StoreSeoSlugGenerateRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
                    Guid storePublicId,
                    StoreSeoSlugValidateRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
                    Guid storePublicId,
                    StoreSeoSlugHistoryQuery query,
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

        
                Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> SetCategoryPrimaryMediaAsync(
                    Guid storePublicId,
                    Guid categoryId,
                    SetCategoryPrimaryMediaRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> ClearCategoryPrimaryMediaAsync(
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
    }
}

