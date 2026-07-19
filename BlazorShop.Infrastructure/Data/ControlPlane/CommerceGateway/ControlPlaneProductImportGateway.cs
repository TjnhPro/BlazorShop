namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;

    public sealed class ControlPlaneProductImportGateway : ControlPlaneCommerceGatewayBase, IControlPlaneProductImportGateway
    {
        public ControlPlaneProductImportGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMultipartAsync<ProductImportUploadResponse>(
                storePublicId,
                "api/commerce/admin/products/import",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportJobListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/imports" + BuildProductImportQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportJobDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/imports/{jobPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportRowsResponse>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/imports/{jobPublicId:D}/rows" + BuildProductImportRowsQuery(query),
                null,
                cancellationToken);
        }
    }
}
