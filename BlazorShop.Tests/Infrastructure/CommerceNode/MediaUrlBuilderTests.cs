namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class MediaUrlBuilderTests
    {
        [Fact]
        public void BuildProductMediaPresetUrl_UsesNamedPresetQuery()
        {
            var mediaPublicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            IProductMediaUrlBuilder builder = new ProductMediaUrlBuilder();

            var url = builder.BuildProductMediaPresetUrl(mediaPublicId, 7, MediaUrlPresetNames.CartLine);

            Assert.Equal(
                "/media/products/11111111-1111-1111-1111-111111111111?w=160&h=160&fit=cover&format=webp&v=7",
                url);
        }

        [Fact]
        public void BuildAssetUrl_WithoutPresetKeepsCanonicalRouteOnly()
        {
            var assetPublicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            ICommerceMediaUrlBuilder builder = new CommerceMediaUrlBuilder();

            var url = builder.BuildAssetUrl(assetPublicId, "Summer Sale.png");

            Assert.Equal("/media/assets/22222222-2222-2222-2222-222222222222/Summer%20Sale.png", url);
        }

        [Fact]
        public void BuildAssetUrl_WithPresetAndVersionAddsStableQuery()
        {
            var assetPublicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            ICommerceMediaUrlBuilder builder = new CommerceMediaUrlBuilder();

            var url = builder.BuildAssetUrl(assetPublicId, "hero.png", 42, MediaUrlPresetNames.ContentBanner);

            Assert.Equal(
                "/media/assets/33333333-3333-3333-3333-333333333333/hero.png?w=1920&h=600&fit=cover&format=webp&v=42",
                url);
        }

        [Fact]
        public void BuildAbsoluteProductMediaPresetUrl_UsesPublicBaseUrl()
        {
            var mediaPublicId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            IProductMediaUrlBuilder builder = new ProductMediaUrlBuilder();

            var url = builder.BuildAbsoluteProductMediaPresetUrl(
                mediaPublicId,
                3,
                "https://store.example.com/shop",
                MediaUrlPresetNames.ProductCard);

            Assert.Equal(
                "https://store.example.com/shop/media/products/44444444-4444-4444-4444-444444444444?w=600&h=600&fit=contain&format=webp&v=3",
                url);
        }

        [Fact]
        public void BuildAbsoluteAssetUrl_UsesPublicBaseUrlAndPreservesQuery()
        {
            var assetPublicId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            ICommerceMediaUrlBuilder builder = new CommerceMediaUrlBuilder();

            var url = builder.BuildAbsoluteAssetUrl(
                assetPublicId,
                "hero image.png",
                "https://store.example.com",
                9,
                MediaUrlPresetNames.ContentCard);

            Assert.Equal(
                "https://store.example.com/media/assets/55555555-5555-5555-5555-555555555555/hero%20image.png?w=800&h=600&fit=cover&format=webp&v=9",
                url);
        }

        [Theory]
        [InlineData("ftp://store.example.com", "/media/products/11111111-1111-1111-1111-111111111111")]
        [InlineData("https://store.example.com", "//evil.example.com/media.png")]
        [InlineData("https://store.example.com", "media/products/11111111-1111-1111-1111-111111111111")]
        public void BuildAbsoluteUrl_RejectsUnsupportedInputs(string publicBaseUrl, string mediaUrl)
        {
            Assert.Throws<ArgumentException>(() => MediaDeliveryUrlPolicy.BuildAbsoluteUrl(publicBaseUrl, mediaUrl));
        }
    }
}
