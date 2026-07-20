namespace BlazorShop.Application.CommerceNode.ProductMedia
{
    using BlazorShop.Application.DTOs.Product;

    public interface IProductGalleryReadService
    {
        Task<IReadOnlyList<ProductGalleryImageDto>> GetStoredProductGalleryAsync(
            Guid storeId,
            Guid productId,
            CancellationToken cancellationToken = default);
    }
}
