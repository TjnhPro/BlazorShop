namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed partial class CommerceNodeDevelopmentSeeder
    {
        private async Task EnsureMediaFixtureFilesAsync(CancellationToken cancellationToken)
        {
            await this.EnsureMediaFixtureFileAsync(
                ProductMediaRootPath,
                "qa-fixtures/default/seo-media-product.png",
                cancellationToken);
            await this.EnsureMediaFixtureFileAsync(
                ProductMediaRootPath,
                "qa-fixtures/qa-s2/isolation-product.png",
                cancellationToken);
            await this.EnsureMediaFixtureFileAsync(
                this.commerceMediaStorageOptions.RootPath,
                "qa-fixtures/default/content-fixture.png",
                cancellationToken);
            await this.EnsureMediaFixtureFileAsync(
                this.commerceMediaStorageOptions.RootPath,
                "qa-fixtures/qa-s2/content-fixture.png",
                cancellationToken);
        }

        private async Task EnsureMediaFixtureFileAsync(
            string rootPath,
            string storagePath,
            CancellationToken cancellationToken)
        {
            if (this.mediaStorageProvider.FileExists(this.hostEnvironment.ContentRootPath, rootPath, storagePath))
            {
                return;
            }

            await this.mediaStorageProvider.WriteAllBytesAsync(
                this.hostEnvironment.ContentRootPath,
                rootPath,
                storagePath,
                QaFixturePngBytes,
                cancellationToken);
        }

        private async Task EnsureProductMediaFixturesAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var storeKey = await this.dbContext.CommerceStores
                .Where(store => store.Id == storeId)
                .Select(store => store.StoreKey)
                .FirstAsync(cancellationToken);

            if (string.Equals(storeKey, IsolationStoreKey, StringComparison.OrdinalIgnoreCase))
            {
                await this.EnsurePrimaryProductMediaAsync(
                    QaS2ProductMediaId,
                    QaS2ProductMediaPublicId,
                    storeId,
                    QaS2ProductId,
                    "qa-fixtures/qa-s2/isolation-product.png",
                    "qa-s2-isolation-product.png",
                    "QA S2 isolation product image",
                    "qa-fixture-product-media-s2",
                    cancellationToken);
                await this.EnsureCommerceMediaAssetAsync(
                    QaS2ContentMediaAssetId,
                    QaS2ContentMediaAssetPublicId,
                    storeId,
                    "qa-fixtures/qa-s2/content-fixture.png",
                    "qa-s2-content-fixture.png",
                    "QA S2 content fixture",
                    "QA S2 content fixture image",
                    "qa-fixture-content-media-s2",
                    cancellationToken);
                return;
            }

            await this.EnsurePrimaryProductMediaAsync(
                SeoMediaProductMediaId,
                SeoMediaProductMediaPublicId,
                storeId,
                SeoMediaProductId,
                "qa-fixtures/default/seo-media-product.png",
                "qa-seo-media-product.png",
                "QA SEO media product image",
                "qa-fixture-product-media-default",
                cancellationToken);
            await this.EnsureCommerceMediaAssetAsync(
                DefaultContentMediaAssetId,
                DefaultContentMediaAssetPublicId,
                storeId,
                "qa-fixtures/default/content-fixture.png",
                "qa-content-fixture.png",
                "QA content fixture",
                "QA content fixture image",
                "qa-fixture-content-media-default",
                cancellationToken);
        }

        private async Task EnsurePrimaryProductMediaAsync(
            Guid id,
            Guid publicId,
            Guid storeId,
            Guid productId,
            string storagePath,
            string fileName,
            string altText,
            string contentHash,
            CancellationToken cancellationToken)
        {
            var product = await this.dbContext.Products.FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);
            if (product is null)
            {
                return;
            }

            var otherPrimaryMedia = await this.dbContext.ProductMedia
                .Where(media => media.StoreId == storeId
                    && media.ProductId == productId
                    && media.Id != id
                    && media.IsPrimary
                    && media.DeletedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var existingPrimary in otherPrimaryMedia)
            {
                existingPrimary.IsPrimary = false;
                existingPrimary.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var media = await this.dbContext.ProductMedia.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (media is null)
            {
                media = new ProductMedia { Id = id };
                this.dbContext.ProductMedia.Add(media);
            }

            media.PublicId = publicId;
            media.StoreId = storeId;
            media.ProductId = productId;
            media.OriginalSourceUrl = null;
            media.OriginalStoragePath = storagePath;
            media.ContentHash = contentHash;
            media.FileName = fileName;
            media.MimeType = "image/png";
            media.Width = 1;
            media.Height = 1;
            media.FileSizeBytes = QaFixturePngBytes.Length;
            media.SortOrder = 0;
            media.IsPrimary = true;
            media.AltText = altText;
            media.Status = ProductMediaStatuses.Stored;
            media.ErrorMessage = null;
            media.Version = 1;
            media.ProcessedAt = DateTimeOffset.UtcNow;
            media.DeletedAt = null;
            media.UpdatedAt = DateTimeOffset.UtcNow;

            product.Image = $"/media/products/{publicId:D}?w=640&fit=contain&format=webp&v=1";
            product.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureCommerceMediaAssetAsync(
            Guid id,
            Guid publicId,
            Guid storeId,
            string storagePath,
            string canonicalFileName,
            string displayName,
            string altText,
            string contentHash,
            CancellationToken cancellationToken)
        {
            var asset = await this.dbContext.CommerceMediaAssets.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (asset is null)
            {
                asset = new CommerceMediaAsset
                {
                    Id = id,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.CommerceMediaAssets.Add(asset);
            }

            asset.PublicId = publicId;
            asset.StoreId = storeId;
            asset.OriginalFileName = canonicalFileName;
            asset.CanonicalFileName = canonicalFileName;
            asset.DisplayName = displayName;
            asset.AltText = altText;
            asset.TitleText = displayName;
            asset.UsageType = CommerceMediaAssetUsageTypes.Content;
            asset.OriginalStoragePath = storagePath;
            asset.ContentHash = contentHash;
            asset.MimeType = "image/png";
            asset.Extension = "png";
            asset.Width = 1;
            asset.Height = 1;
            asset.FileSizeBytes = QaFixturePngBytes.Length;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
