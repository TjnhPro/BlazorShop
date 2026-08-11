namespace BlazorShop.Tests.Application.CommerceNode.Media
{
    using BlazorShop.Application.CommerceNode.Media;

    using Xunit;

    public sealed class MediaTransformPolicyTests
    {
        [Fact]
        public void NormalizeProductQuery_DefaultsToCurrentProductTransform()
        {
            var result = MediaTransformPolicy.NormalizeProductQuery(null, null, null, null);

            Assert.True(result.Success);
            Assert.Equal(1000, result.Value.Width);
            Assert.Null(result.Value.Height);
            Assert.Equal("contain", result.Value.Fit);
            Assert.Equal("webp", result.Value.Format);
        }

        [Fact]
        public void NormalizeProductQuery_ClampsDimensionsAndPreservesExistingValues()
        {
            var result = MediaTransformPolicy.NormalizeProductQuery(3000, 2400, "COVER", "PNG");

            Assert.True(result.Success);
            Assert.Equal(2000, result.Value.Width);
            Assert.Equal(2000, result.Value.Height);
            Assert.Equal("cover", result.Value.Fit);
            Assert.Equal("png", result.Value.Format);
        }

        [Theory]
        [InlineData("inside", "Media fit is invalid.")]
        [InlineData("contain", null)]
        [InlineData("max", null)]
        public void NormalizeProductQuery_ValidatesProductFits(string fit, string? expectedError)
        {
            var result = MediaTransformPolicy.NormalizeProductQuery(320, null, fit, "webp");

            Assert.Equal(expectedError is null, result.Success);
            Assert.Equal(expectedError, result.Message);
        }

        [Fact]
        public void NormalizeAssetQuery_DefaultsToOriginalInsideWithoutTransform()
        {
            var result = MediaTransformPolicy.NormalizeAssetQuery(null, null, null, null, 800, 600, hasTransformQuery: false);

            Assert.True(result.Success);
            Assert.Null(result.Value.Width);
            Assert.Null(result.Value.Height);
            Assert.Equal("inside", result.Value.Fit);
            Assert.Equal("original", result.Value.Format);
        }

        [Fact]
        public void NormalizeAssetQuery_UsesSourceSizeForFormatOnlyTransform()
        {
            var result = MediaTransformPolicy.NormalizeAssetQuery(null, null, null, "webp", 800, 600, hasTransformQuery: true);

            Assert.True(result.Success);
            Assert.Equal(800, result.Value.Width);
            Assert.Equal(600, result.Value.Height);
            Assert.Equal("webp", result.Value.Format);
        }

        [Fact]
        public void NormalizeAssetQuery_KeepsGenericAssetsClampedToSourceDimensions()
        {
            var result = MediaTransformPolicy.NormalizeAssetQuery(800, 200, "contain", "png", 300, 100, hasTransformQuery: true);

            Assert.True(result.Success);
            Assert.Equal(300, result.Value.Width);
            Assert.Equal(100, result.Value.Height);
            Assert.False(result.Value.Extend);
        }

        [Fact]
        public void NormalizeAssetQuery_AllowsBrandingCanvasToEnlargeAndExtend()
        {
            var result = MediaTransformPolicy.NormalizeAssetQuery(
                800,
                200,
                "contain",
                "png",
                300,
                100,
                hasTransformQuery: true,
                extend: true,
                allowEnlarge: true);

            Assert.True(result.Success);
            Assert.Equal(800, result.Value.Width);
            Assert.Equal(200, result.Value.Height);
            Assert.True(result.Value.Extend);
        }

        [Fact]
        public void NormalizeAssetQuery_RejectsCanvasExtensionForGenericAssets()
        {
            var result = MediaTransformPolicy.NormalizeAssetQuery(
                800,
                200,
                "contain",
                "png",
                300,
                100,
                hasTransformQuery: true,
                extend: true);

            Assert.False(result.Success);
            Assert.Equal("Media canvas extension is only supported for branding assets.", result.Message);
        }

        [Fact]
        public void NormalizeAssetQuery_RejectsTooLargeOutput()
        {
            var result = MediaTransformPolicy.NormalizeAssetQuery(4096, 4096, "cover", "webp", null, null, hasTransformQuery: true);

            Assert.False(result.Success);
            Assert.Equal("Media transform output is too large.", result.Message);
        }

        [Fact]
        public void MediaUrlPresets_ExposeApprovedPresetNames()
        {
            var preset = MediaUrlPresets.Get(MediaUrlPresetNames.CategoryCard);

            Assert.Equal("category-card", preset.Name);
            Assert.Equal(600, preset.Width);
            Assert.Equal(400, preset.Height);
            Assert.Equal("cover", preset.Fit);
            Assert.Equal("webp", preset.Format);
        }

        [Fact]
        public void MediaUrlPresets_NormalizeBrandingCanvasesWithoutChangingProductPresets()
        {
            var logo = MediaUrlPresets.Get(MediaUrlPresetNames.BrandLogo);
            var favicon = MediaUrlPresets.Get(MediaUrlPresetNames.BrandFavicon);
            var product = MediaUrlPresets.Get(MediaUrlPresetNames.ProductCard);

            Assert.Equal((800, 200, "contain", "png", true), (logo.Width, logo.Height, logo.Fit, logo.Format, logo.Extend));
            Assert.Equal((512, 512, "contain", "png", true), (favicon.Width, favicon.Height, favicon.Fit, favicon.Format, favicon.Extend));
            Assert.Equal((600, 600, "contain", "webp", false), (product.Width, product.Height, product.Fit, product.Format, product.Extend));
        }
    }
}
