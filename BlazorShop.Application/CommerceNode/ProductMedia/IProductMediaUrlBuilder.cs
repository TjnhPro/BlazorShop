namespace BlazorShop.Application.CommerceNode.ProductMedia
{
    public interface IProductMediaUrlBuilder
    {
        string BuildProductMediaUrl(Guid mediaPublicId, int version, ProductMediaUrlOptions? options = null);
    }

    public sealed record ProductMediaUrlOptions(
        int? Width = 1000,
        int? Height = null,
        string Fit = "contain",
        string Format = "webp");
}
