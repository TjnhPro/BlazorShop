namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CommerceNodeProductGalleryReadServiceTests
    {
        [Fact]
        public async Task GetStoredProductGalleryAsync_ReturnsStoreScopedStoredMediaInGalleryOrder()
        {
            var storeId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var primaryPublicId = Guid.Parse("00000000-0000-0000-0000-000000000101");
            var secondaryPublicId = Guid.Parse("00000000-0000-0000-0000-000000000102");

            await using var context = CreateContext();
            context.ProductMedia.AddRange(
                CreateMedia(storeId, productId, secondaryPublicId, 1, false, "Secondary alt", 1),
                CreateMedia(storeId, productId, primaryPublicId, 99, true, "Primary alt", 2, version: 2),
                CreateMedia(storeId, productId, Guid.Parse("00000000-0000-0000-0000-000000000103"), 2, false, "Deleted", 3, deleted: true),
                CreateMedia(storeId, productId, Guid.Parse("00000000-0000-0000-0000-000000000104"), 3, false, "Pending", 4, status: ProductMediaStatuses.Pending),
                CreateMedia(otherStoreId, productId, Guid.Parse("00000000-0000-0000-0000-000000000105"), 0, true, "Other store", 5));
            await context.SaveChangesAsync();

            var service = new CommerceNodeProductGalleryReadService(context, new ProductMediaUrlBuilder());

            var result = await service.GetStoredProductGalleryAsync(storeId, productId);

            Assert.Collection(
                result,
                item =>
                {
                    Assert.Equal(primaryPublicId, item.PublicId);
                    Assert.True(item.IsPrimary);
                    Assert.Equal("Primary alt", item.AltText);
                    Assert.Equal(2, item.Version);
                    Assert.Equal($"/media/products/{primaryPublicId:D}?w=1000&h=1000&fit=contain&format=webp&v=2", item.ImageUrl);
                    Assert.Equal($"/media/products/{primaryPublicId:D}?w=600&h=600&fit=contain&format=webp&v=2", item.ThumbnailUrl);
                    Assert.Equal(item.ImageUrl, item.FullSizeUrl);
                },
                item =>
                {
                    Assert.Equal(secondaryPublicId, item.PublicId);
                    Assert.False(item.IsPrimary);
                    Assert.Equal("Secondary alt", item.AltText);
                    Assert.Equal(1, item.SortOrder);
                });
            Assert.All(result, item =>
            {
                Assert.DoesNotContain("storage", item.ImageUrl, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("bucket", item.ImageUrl, StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public void DevelopmentSeeder_KeepsIncrementalThreeImageGalleryFixture()
        {
            var repositoryRoot = FindRepositoryRoot();
            var seederSource = File.ReadAllText(Path.Combine(
                repositoryRoot,
                "BlazorShop.Infrastructure",
                "Data",
                "CommerceNode",
                "CommerceNodeDevelopmentSeeder.cs"));
            var storeSeedSource = File.ReadAllText(Path.Combine(
                repositoryRoot,
                "BlazorShop.Infrastructure",
                "Data",
                "CommerceNode",
                "CommerceNodeDevelopmentSeeder.StoreSeed.cs"));
            var mediaSeedSource = File.ReadAllText(Path.Combine(
                repositoryRoot,
                "BlazorShop.Infrastructure",
                "Data",
                "CommerceNode",
                "CommerceNodeDevelopmentSeeder.MediaSeed.cs"));

            Assert.Contains("EnsureIncrementalQaSeedDataAsync", seederSource);
            Assert.Contains("SeoMediaProductGallerySecondMediaPublicId", seederSource);
            Assert.Contains("SeoMediaProductGalleryThirdMediaPublicId", seederSource);
            Assert.Contains("defaultGalleryCount >= 3", storeSeedSource);
            Assert.Contains("qa-fixtures/default/seo-media-product-alt-1.png", mediaSeedSource);
            Assert.Contains("qa-fixtures/default/seo-media-product-alt-2.png", mediaSeedSource);
            Assert.Contains("sortOrder: 10", mediaSeedSource);
            Assert.Contains("sortOrder: 20", mediaSeedSource);
            Assert.Contains("isPrimary: false", mediaSeedSource);
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-product-gallery-read-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate BlazorShop repository root.");
        }

        private static ProductMedia CreateMedia(
            Guid storeId,
            Guid productId,
            Guid publicId,
            int sortOrder,
            bool isPrimary,
            string altText,
            int createdAtDay,
            int version = 1,
            string status = ProductMediaStatuses.Stored,
            bool deleted = false)
        {
            return new ProductMedia
            {
                Id = Guid.NewGuid(),
                PublicId = publicId,
                StoreId = storeId,
                ProductId = productId,
                OriginalStoragePath = $"private/{publicId:D}.png",
                Status = status,
                SortOrder = sortOrder,
                IsPrimary = isPrimary,
                AltText = altText,
                Width = 1200,
                Height = 900,
                Version = version,
                CreatedAt = new DateTimeOffset(2026, 7, createdAtDay, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 7, createdAtDay, 0, 0, 0, TimeSpan.Zero),
                DeletedAt = deleted ? new DateTimeOffset(2026, 7, createdAtDay, 1, 0, 0, TimeSpan.Zero) : null,
            };
        }
    }
}
