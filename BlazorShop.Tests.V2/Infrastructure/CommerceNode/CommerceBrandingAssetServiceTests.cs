namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class CommerceBrandingAssetServiceTests
    {
        [Fact]
        public async Task UploadAsync_StoresBrandingUsageAndReturnsFixedLogoUrl()
        {
            var media = new FakeMediaAssetService();
            var service = new CommerceBrandingAssetService(media, new CommerceMediaUrlBuilder());

            var result = await service.UploadAsync(new CommerceBrandingAssetUploadRequest(
                CommerceBrandingAssetSlots.Logo,
                CreateUpload("brandlogo.png", "image/png")));

            Assert.True(result.Success);
            Assert.Equal(CommerceBrandingAssetSlots.Logo, result.Value!.Slot);
            Assert.Equal(CommerceMediaAssetUsageTypes.Branding, media.LastMetadataRequest!.UsageType);
            Assert.Contains("w=800&h=200&fit=contain&format=png&extend=true", result.Value.EffectiveUrl);
        }

        [Theory]
        [InlineData("brand.jpg")]
        [InlineData("brand.gif")]
        [InlineData("brand.ico")]
        public async Task UploadAsync_RejectsNonBrandingFormats(string fileName)
        {
            var media = new FakeMediaAssetService();
            var service = new CommerceBrandingAssetService(media, new CommerceMediaUrlBuilder());

            var result = await service.UploadAsync(new CommerceBrandingAssetUploadRequest(
                CommerceBrandingAssetSlots.Favicon,
                CreateUpload(fileName, "image/png")));

            Assert.False(result.Success);
            Assert.Equal(ApplicationErrorKind.Validation, result.Error!.Kind);
            Assert.Null(media.LastUploadRequest);
        }

        [Fact]
        public async Task UploadAsync_UsesSquareFaviconPreset()
        {
            var media = new FakeMediaAssetService();
            var service = new CommerceBrandingAssetService(media, new CommerceMediaUrlBuilder());

            var result = await service.UploadAsync(new CommerceBrandingAssetUploadRequest(
                CommerceBrandingAssetSlots.Favicon,
                CreateUpload("brandfavicon.webp", "image/webp")));

            Assert.True(result.Success);
            Assert.Contains("w=512&h=512&fit=contain&format=png&extend=true", result.Value!.EffectiveUrl);
        }

        private static CommerceMediaAssetUploadRequest CreateUpload(string fileName, string contentType)
        {
            return new CommerceMediaAssetUploadRequest(
                new MemoryStream([1, 2, 3]),
                fileName,
                contentType,
                3);
        }

        private sealed class FakeMediaAssetService : ICommerceMediaAssetService
        {
            private static readonly CommerceMediaAssetDto Asset = new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "brand.png",
                "brand.png",
                "Brand",
                "Brand",
                null,
                CommerceMediaAssetUsageTypes.Content,
                "/media/assets/11111111-1111-1111-1111-111111111111/brand.png",
                "image/png",
                "png",
                300,
                100,
                3,
                7,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);

            public CommerceMediaAssetUploadRequest? LastUploadRequest { get; private set; }

            public CommerceMediaAssetMetadataRequest? LastMetadataRequest { get; private set; }

            public Task<ApplicationResult<CommerceMediaAssetDto>> UploadAsync(CommerceMediaAssetUploadRequest request, CancellationToken cancellationToken = default)
            {
                this.LastUploadRequest = request;
                return Task.FromResult(ApplicationResult<CommerceMediaAssetDto>.Succeeded(Asset));
            }

            public Task<ApplicationResult<CommerceMediaAssetDto>> UpdateMetadataAsync(Guid assetPublicId, CommerceMediaAssetMetadataRequest request, CancellationToken cancellationToken = default)
            {
                this.LastMetadataRequest = request;
                return Task.FromResult(ApplicationResult<CommerceMediaAssetDto>.Succeeded(Asset with { UsageType = request.UsageType! }));
            }

            public Task<ApplicationResult<CommerceMediaAssetListResponse>> ListAsync(CommerceMediaAssetListQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<ApplicationResult<CommerceMediaAssetDto>> GetAsync(Guid assetPublicId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<ApplicationResult<CommerceMediaAssetDto>> ReplaceAsync(Guid assetPublicId, CommerceMediaAssetUploadRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<ApplicationResult<object>> DeleteAsync(Guid assetPublicId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
