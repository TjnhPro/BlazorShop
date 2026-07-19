namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;
    using BlazorShop.Application.DTOs.Seo;

    public sealed class ControlPlaneProductSeoGateway : ControlPlaneCommerceGatewayBase, IControlPlaneProductSeoGateway
    {
        public ControlPlaneProductSeoGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ApplicationResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/seo",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            return this.SendApplicationAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/seo",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/generate",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/validate",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<IReadOnlyList<StoreSeoSlugHistoryDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/seo/slugs/history" + BuildSeoSlugHistoryQuery(query),
                null,
                cancellationToken);
        }
    }
}
