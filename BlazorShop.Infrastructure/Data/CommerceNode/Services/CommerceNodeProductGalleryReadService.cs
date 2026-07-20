namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeProductGalleryReadService : IProductGalleryReadService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IProductMediaUrlBuilder urlBuilder;

        public CommerceNodeProductGalleryReadService(
            CommerceNodeDbContext context,
            IProductMediaUrlBuilder urlBuilder)
        {
            this.context = context;
            this.urlBuilder = urlBuilder;
        }

        public async Task<IReadOnlyList<ProductGalleryImageDto>> GetStoredProductGalleryAsync(
            Guid storeId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty || productId == Guid.Empty)
            {
                return [];
            }

            var mediaItems = await this.context.ProductMedia
                .AsNoTracking()
                .Where(media => media.StoreId == storeId
                    && media.ProductId == productId
                    && media.DeletedAt == null
                    && media.Status == ProductMediaStatuses.Stored)
                .OrderByDescending(media => media.IsPrimary)
                .ThenBy(media => media.SortOrder)
                .ThenBy(media => media.CreatedAt)
                .ThenBy(media => media.Id)
                .Select(media => new GalleryMediaProjection(
                    media.PublicId,
                    media.AltText,
                    media.SortOrder,
                    media.IsPrimary,
                    media.Width,
                    media.Height,
                    media.Version))
                .ToListAsync(cancellationToken);

            return mediaItems.Select(this.Map).ToArray();
        }

        private ProductGalleryImageDto Map(GalleryMediaProjection media)
        {
            var version = Math.Max(1, media.Version);
            return new ProductGalleryImageDto(
                media.PublicId,
                this.urlBuilder.BuildProductMediaPresetUrl(media.PublicId, version, MediaUrlPresetNames.ProductDetail),
                this.urlBuilder.BuildProductMediaPresetUrl(media.PublicId, version, MediaUrlPresetNames.ProductCard),
                this.urlBuilder.BuildProductMediaPresetUrl(media.PublicId, version, MediaUrlPresetNames.ProductDetail),
                media.AltText,
                media.SortOrder,
                media.IsPrimary,
                media.Width,
                media.Height,
                version);
        }

        private sealed record GalleryMediaProjection(
            Guid PublicId,
            string? AltText,
            int SortOrder,
            bool IsPrimary,
            int? Width,
            int? Height,
            int Version);
    }
}
