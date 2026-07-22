extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using BlazorShop.Application.DTOs.Seo;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontIndexingPolicyTests
    {
        [Theory]
        [InlineData("/my-cart")]
        [InlineData("/checkout?error=empty")]
        [InlineData("/signin?returnUrl=/checkout")]
        [InlineData("/register")]
        [InlineData("/payment-success?paymentAttemptId=123")]
        [InlineData("/payment-cancel")]
        [InlineData("/maintenance?reason=closed")]
        public void IsPrivateNoIndexPath_ReturnsTrueForPrivateAndOperationalRoutes(string path)
        {
            Assert.True(StorefrontIndexingPolicy.IsPrivateNoIndexPath(path));
        }

        [Theory]
        [InlineData("/category/apparel?utm_source=newsletter", "/category/apparel")]
        [InlineData("/category/apparel/?sortBy=price_desc", "/category/apparel")]
        [InlineData("/product/catalog-qa-t-shirt#details", "/product/catalog-qa-t-shirt")]
        [InlineData("/", "/")]
        public void NormalizeCanonicalPath_StripsQueryFragmentAndTrailingSlash(string path, string expected)
        {
            Assert.Equal(expected, StorefrontIndexingPolicy.NormalizeCanonicalPath(path));
        }

        [Fact]
        public void ApplySearchMetadata_NoIndexesSearchAndSuppressesCanonical()
        {
            var metadata = new SeoMetadataDto
            {
                CanonicalUrl = "https://shop.example.com/search?q=shoes",
                RobotsIndex = true,
                RobotsFollow = false,
            };

            StorefrontIndexingPolicy.ApplySearchMetadata(metadata);

            Assert.False(metadata.RobotsIndex);
            Assert.True(metadata.RobotsFollow);
            Assert.Null(metadata.CanonicalUrl);
        }
    }
}
