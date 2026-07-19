namespace BlazorShop.Application.ControlPlane.CommerceGateway.Products
{
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.ControlPlane.Catalog;

    public interface IControlPlaneProductImportGateway
    {
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
    }
}
