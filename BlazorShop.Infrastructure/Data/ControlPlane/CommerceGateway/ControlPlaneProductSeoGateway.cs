namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;
    using BlazorShop.Application.DTOs.Seo;

    public sealed class ControlPlaneProductSeoGateway : ControlPlaneCommerceGatewayBase, IControlPlaneProductSeoGateway
    {
        public ControlPlaneProductSeoGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/seo",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            return this.SendAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/seo",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/generate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/validate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreSeoSlugHistoryDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/seo/slugs/history" + BuildSeoSlugHistoryQuery(query),
                null,
                cancellationToken);
        }
    }
}
