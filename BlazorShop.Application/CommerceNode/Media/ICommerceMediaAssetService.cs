namespace BlazorShop.Application.CommerceNode.Media
{
    public interface ICommerceMediaAssetService
    {
        Task<CommerceMediaAssetOperationResult<CommerceMediaAssetListResponse>> ListAsync(
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default);

        Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> GetAsync(
            Guid assetPublicId,
            CancellationToken cancellationToken = default);

        Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> UploadAsync(
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> UpdateMetadataAsync(
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceMediaAssetOperationResult<CommerceMediaAssetDto>> ReplaceAsync(
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<CommerceMediaAssetOperationResult<object>> DeleteAsync(
            Guid assetPublicId,
            CancellationToken cancellationToken = default);
    }
}
