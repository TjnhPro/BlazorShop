namespace BlazorShop.Application.CommerceNode.ProductMedia
{
    public interface IProductMediaUrlBuilder
    {
        string BuildProductMediaUrl(Guid mediaPublicId, int version, ProductMediaUrlOptions? options = null);

        string BuildProductMediaPresetUrl(Guid mediaPublicId, int version, string presetName);

        string BuildAbsoluteProductMediaUrl(
            Guid mediaPublicId,
            int version,
            string publicBaseUrl,
            ProductMediaUrlOptions? options = null);

        string BuildAbsoluteProductMediaPresetUrl(
            Guid mediaPublicId,
            int version,
            string publicBaseUrl,
            string presetName);
    }

    public sealed record ProductMediaUrlOptions(
        int? Width = 1000,
        int? Height = null,
        string Fit = "contain",
        string Format = "webp");
}
