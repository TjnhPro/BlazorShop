namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    using Xunit;

    public sealed class CommerceMediaAssetUsageTypeTests
    {
        [Theory]
        [InlineData(null, CommerceMediaAssetUsageTypes.Content)]
        [InlineData("", CommerceMediaAssetUsageTypes.Content)]
        [InlineData(" BRANDING ", CommerceMediaAssetUsageTypes.Branding)]
        [InlineData("category", CommerceMediaAssetUsageTypes.Category)]
        public void NormalizeOrDefault_ReturnsSupportedLowercaseValue(string? value, string expected)
        {
            Assert.Equal(expected, CommerceMediaAssetUsageTypes.NormalizeOrDefault(value));
        }

        [Fact]
        public async Task ListAsync_FiltersByUsageType()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            await using var context = CreateContext();
            SeedAsset(context, storeId, "content.jpg", CommerceMediaAssetUsageTypes.Content);
            SeedAsset(context, storeId, "category.jpg", CommerceMediaAssetUsageTypes.Category);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.ListAsync(new CommerceMediaAssetListQuery(UsageType: CommerceMediaAssetUsageTypes.Category));

            Assert.True(result.Success);
            var item = Assert.Single(result.Value!.Items);
            Assert.Equal(CommerceMediaAssetUsageTypes.Category, item.UsageType);
            Assert.Equal("category.jpg", item.CanonicalFileName);
        }

        [Fact]
        public async Task UpdateMetadataAsync_UpdatesUsageType()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var assetPublicId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            await using var context = CreateContext();
            SeedAsset(context, storeId, "logo.png", CommerceMediaAssetUsageTypes.Content, assetPublicId);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.UpdateMetadataAsync(
                assetPublicId,
                new CommerceMediaAssetMetadataRequest("Logo", "Logo alt", null, "BRANDING"));

            Assert.True(result.Success);
            Assert.Equal(CommerceMediaAssetUsageTypes.Branding, result.Value!.UsageType);
        }

        [Fact]
        public async Task UpdateMetadataAsync_RejectsUnsupportedUsageType()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var assetPublicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            await using var context = CreateContext();
            SeedAsset(context, storeId, "bad.png", CommerceMediaAssetUsageTypes.Content, assetPublicId);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.UpdateMetadataAsync(
                assetPublicId,
                new CommerceMediaAssetMetadataRequest("Bad", "Bad alt", null, "unknown"));

            Assert.False(result.Success);
            Assert.Equal(ApplicationErrorKind.Validation, result.Error!.Kind);
        }

        [Fact]
        public async Task DeleteAsync_WhenAssetIsAssignedToCategory_ReturnsConflict()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var categoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var assetPublicId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            await using var context = CreateContext();
            var asset = SeedAsset(context, storeId, "assigned.jpg", CommerceMediaAssetUsageTypes.Category, assetPublicId);
            context.Categories.Add(new Category
            {
                Id = categoryId,
                StoreId = storeId,
                Name = "Assigned category",
                Slug = "assigned-category",
                IsPublished = true,
            });
            context.CategoryMediaAssignments.Add(new CategoryMediaAssignment
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                CategoryId = categoryId,
                MediaAssetId = asset.Id,
                IsPrimary = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.DeleteAsync(assetPublicId);

            Assert.False(result.Success);
            Assert.Equal(ApplicationErrorKind.Conflict, result.Error!.Kind);
            Assert.True(await context.CommerceMediaAssets.AnyAsync(media => media.PublicId == assetPublicId));
        }

        private static CommerceMediaAssetService CreateService(CommerceNodeDbContext context, Guid storeId)
        {
            return new CommerceMediaAssetService(
                context,
                new FixedCommerceStoreContext(storeId),
                Options.Create(new CommerceMediaStorageOptions()),
                new FakeHostEnvironment(),
                new LocalMediaStorageProvider(),
                new CommerceMediaUrlBuilder());
        }

        private static CommerceMediaAsset SeedAsset(
            CommerceNodeDbContext context,
            Guid storeId,
            string canonicalFileName,
            string usageType,
            Guid? publicId = null)
        {
            var assetPublicId = publicId ?? Guid.NewGuid();
            var asset = new CommerceMediaAsset
            {
                Id = Guid.NewGuid(),
                PublicId = assetPublicId,
                StoreId = storeId,
                OriginalFileName = canonicalFileName,
                CanonicalFileName = canonicalFileName,
                DisplayName = canonicalFileName,
                AltText = canonicalFileName,
                UsageType = usageType,
                OriginalStoragePath = $"stores/{storeId:D}/{assetPublicId:D}/original.jpg",
                ContentHash = assetPublicId.ToString("N"),
                MimeType = "image/jpeg",
                Extension = ".jpg",
                Width = 600,
                Height = 400,
                FileSizeBytes = 1024,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            context.CommerceMediaAssets.Add(asset);
            return asset;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-media-usage-type-{Guid.NewGuid():N}")
                .Options;
            return new CommerceNodeDbContext(options);
        }

        private sealed class FixedCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedCommerceStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ApplicationResult<Guid>(true, "Store resolved.", this.storeId));
            }
        }

        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
