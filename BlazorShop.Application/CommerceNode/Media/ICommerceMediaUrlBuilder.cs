namespace BlazorShop.Application.CommerceNode.Media
{
    public interface ICommerceMediaUrlBuilder
    {
        string BuildAssetUrl(
            Guid assetPublicId,
            string canonicalFileName,
            long? version = null,
            string? presetName = null);

        string BuildAbsoluteAssetUrl(
            Guid assetPublicId,
            string canonicalFileName,
            string publicBaseUrl,
            long? version = null,
            string? presetName = null);
    }
}
