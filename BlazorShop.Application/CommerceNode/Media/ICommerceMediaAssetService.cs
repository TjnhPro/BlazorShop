namespace BlazorShop.Application.CommerceNode.Media
{
    using BlazorShop.Application.Common.Results;

    public interface ICommerceMediaAssetService
    {
        Task<ApplicationResult<CommerceMediaAssetListResponse>> ListAsync(
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceMediaAssetDto>> GetAsync(
            Guid assetPublicId,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceMediaAssetDto>> UploadAsync(
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceMediaAssetDto>> UpdateMetadataAsync(
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceMediaAssetDto>> ReplaceAsync(
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<object>> DeleteAsync(
            Guid assetPublicId,
            CancellationToken cancellationToken = default);
    }
}
