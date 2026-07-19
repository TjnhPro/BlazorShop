namespace BlazorShop.Application.ControlPlane.CommerceGateway.Products
{
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Seo;

    public interface IControlPlaneProductSeoGateway
    {
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
    }
}
