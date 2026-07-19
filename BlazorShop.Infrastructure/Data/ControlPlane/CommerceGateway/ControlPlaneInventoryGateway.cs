namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Domain.Contracts;

    public sealed class ControlPlaneInventoryGateway : ControlPlaneCommerceGatewayBase, IControlPlaneInventoryGateway
    {
        public ControlPlaneInventoryGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<AdminInventoryItemDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/inventory" + BuildInventoryQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<AdminInventoryItemDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/inventory/products/{productId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<AdminInventoryVariantDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/inventory/variants/{variantId:D}",
                request,
                cancellationToken);
        }
    }
}
