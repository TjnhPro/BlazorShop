namespace BlazorShop.Application.CommerceNode.Media
{
    using BlazorShop.Application.Common.Results;

    public interface ICommerceBrandingAssetService
    {
        Task<ApplicationResult<CommerceBrandingAssetResponse>> UploadAsync(
            CommerceBrandingAssetUploadRequest request,
            CancellationToken cancellationToken = default);
    }
}
