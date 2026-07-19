namespace BlazorShop.Application.ControlPlane.CommerceGateway.Media
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public interface IControlPlaneMediaGateway
    {
        
                Task<ApplicationResult<ProductMediaListResponse>> ListProductMediaAsync(
                    Guid storePublicId,
                    Guid productId,
                    ProductMediaListQuery query,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
                    Guid storePublicId,
                    CommerceMediaAssetListQuery query,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<CommerceMediaAssetDto>> GetMediaAssetAsync(
                    Guid storePublicId,
                    Guid assetPublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
                    Guid storePublicId,
                    CommerceMediaAssetUploadRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
                    Guid storePublicId,
                    Guid assetPublicId,
                    CommerceMediaAssetMetadataRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
                    Guid storePublicId,
                    Guid assetPublicId,
                    CommerceMediaAssetUploadRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<object>> DeleteMediaAssetAsync(
                    Guid storePublicId,
                    Guid assetPublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ImportProductMediaResponse>> ImportProductMediaAsync(
                    Guid storePublicId,
                    Guid productId,
                    ImportProductMediaRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
                    Guid storePublicId,
                    Guid productId,
                    UpdateProductMediaOrderRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
                    Guid storePublicId,
                    Guid productId,
                    Guid mediaPublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ProductMediaListResponse>> DeleteProductMediaAsync(
                    Guid storePublicId,
                    Guid productId,
                    Guid mediaPublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ImportProductMediaResponse>> RetryProductMediaAsync(
                    Guid storePublicId,
                    Guid productId,
                    Guid mediaPublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ApplicationMediaContent>> GetProductMediaPreviewAsync(
                    Guid storePublicId,
                    Guid mediaPublicId,
                    ProductMediaPreviewQuery query,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<ApplicationMediaContent>> GetMediaAssetPreviewAsync(
                    Guid storePublicId,
                    Guid assetPublicId,
                    string canonicalFileName,
                    MediaAssetPreviewQuery query,
                    CancellationToken cancellationToken = default);
    }
}

