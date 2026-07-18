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
    public sealed class ControlPlaneMediaGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Media.IControlPlaneMediaGateway
    {
        public ControlPlaneMediaGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/media" + BuildPageQuery(query.PageNumber, query.PageSize),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ImportProductMediaResponse>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/import",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/media/order",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}/primary",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ImportProductMediaResponse>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}/retry",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceMediaAssetListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/media/assets" + BuildMediaAssetListQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> GetMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceMediaAssetDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/media/assets/{assetPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAssetMultipartAsync<CommerceMediaAssetDto>(
                storePublicId,
                "api/commerce/admin/media/assets",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceMediaAssetDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/media/assets/{assetPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAssetMultipartAsync<CommerceMediaAssetDto>(
                storePublicId,
                $"api/commerce/admin/media/assets/{assetPublicId:D}/replace",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/media/assets/{assetPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceMediaResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAsync(
                storePublicId,
                $"api/commerce/admin/media/products/{mediaPublicId:D}" + BuildMediaPreviewQuery(query),
                cancellationToken);
        }

        public Task<ControlPlaneCommerceMediaResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAsync(
                storePublicId,
                $"api/commerce/admin/media/assets/{assetPublicId:D}/preview" + BuildMediaAssetPreviewQuery(query),
                cancellationToken);
        }
    }
}

