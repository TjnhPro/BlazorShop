namespace BlazorShop.ControlPlane.Web.Services.Commerce
{
    using System.Globalization;
    using System.Net.Http.Headers;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Domain.Contracts;

        public sealed class ControlPlaneMediaClient : ControlPlaneCommerceClientBase, IControlPlaneMediaClient
    {
        public ControlPlaneMediaClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media") + BuildPageQuery(query.PageNumber, query.PageSize),
                "Unable to load product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<ImportProductMediaRequest, ImportProductMediaResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/import"),
                request,
                "Unable to import product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateProductMediaOrderRequest, ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/order"),
                request,
                "Unable to update product media order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<ProductMediaDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/primary"),
                "Unable to set primary product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.DeletePrivateAsync<ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}"),
                "Unable to delete product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<ImportProductMediaResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/retry"),
                "Unable to retry product media.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/preview") + BuildMediaPreviewQuery(query),
                "Unable to load product media preview.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<CommerceMediaAssetListResponse>(
                CommerceRoute(storePublicId, "media/assets") + BuildMediaAssetListQuery(query),
                "Unable to load media assets.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            using var form = BuildMediaAssetForm(content, fileName, contentType);
            return await this.ApiClient.PostPrivateMultipartAsync<CommerceMediaAssetDto>(
                CommerceRoute(storePublicId, "media/assets"),
                form,
                "Unable to upload media asset.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<CommerceMediaAssetMetadataRequest, CommerceMediaAssetDto>(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}"),
                request,
                "Unable to update media asset.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            Stream content,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            using var form = BuildMediaAssetForm(content, fileName, contentType);
            return await this.ApiClient.PostPrivateMultipartAsync<CommerceMediaAssetDto>(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}/replace"),
                form,
                "Unable to replace media asset.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}"),
                "Unable to delete media asset.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"media/assets/{assetPublicId:D}/preview") + BuildMediaAssetPreviewQuery(canonicalFileName, query),
                "Unable to load media asset preview.",
                cancellationToken);
        }
    }
}

