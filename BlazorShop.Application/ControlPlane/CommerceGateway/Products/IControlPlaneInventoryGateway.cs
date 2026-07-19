namespace BlazorShop.Application.ControlPlane.CommerceGateway.Products
{
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Domain.Contracts;

    public interface IControlPlaneInventoryGateway
    {
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
