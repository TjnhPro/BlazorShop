namespace BlazorShop.Application.ControlPlane.Catalog
{
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
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

        Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
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

        Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
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

        Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
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

    public sealed record ControlPlaneCommerceCatalogResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneCommerceCatalogFailure? Failure = null,
        int? HttpStatusCode = null);

    public enum ControlPlaneCommerceCatalogFailure
    {
        Validation,
        NotFound,
        RemoteFailure
    }
}
