namespace BlazorShop.Tests.Application.Services
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;

    using Xunit;

    public class SeoMetadataBuilderTests
    {
        private readonly ISeoMetadataBuilder _builder = new SeoMetadataBuilder();

        [Fact]
        public void Build_WhenEntitySeoIsPartial_ComposesMetadataFromEntityAndGlobalDefaults()
        {
            var request = new SeoMetadataBuildRequest
            {
                PageTitle = "Running Shoes",
                RelativePath = "/products/running-shoes",
                PageSeo = new SeoFieldsDto
                {
                    MetaTitle = "Best Running Shoes",
                    OgImage = "/images/og/shoes.png",
                    RobotsFollow = false,
                },
                Settings = new SeoSettingsDto
                {
                    SiteName = "BlazorShop",
                    DefaultTitleSuffix = "| BlazorShop",
                    DefaultMetaDescription = "Shop the latest catalog.",
                    DefaultOgImage = "https://cdn.example.com/default-og.png",
                    BaseCanonicalUrl = "https://shop.example.com",
                },
            };

            var result = this._builder.Build(request);

            Assert.Equal("Best Running Shoes | BlazorShop", result.Title);
            Assert.Equal("Shop the latest catalog.", result.MetaDescription);
            Assert.Equal("https://shop.example.com/products/running-shoes", result.CanonicalUrl);
            Assert.Equal("Best Running Shoes | BlazorShop", result.OgTitle);
            Assert.Equal("Shop the latest catalog.", result.OgDescription);
            Assert.Equal("https://shop.example.com/images/og/shoes.png", result.OgImage);
            Assert.Equal("BlazorShop", result.SiteName);
            Assert.True(result.RobotsIndex);
            Assert.False(result.RobotsFollow);
        }

        [Fact]
        public void Build_WhenCanonicalAndOpenGraphAreSuppressed_OmitsThoseFields()
        {
            var request = new SeoMetadataBuildRequest
            {
                PageTitle = "Missing Product",
                RelativePath = "/product/missing-product",
                SuppressCanonicalUrl = true,
                SuppressOpenGraph = true,
                PageSeo = new SeoFieldsDto
                {
                    MetaDescription = "We couldn't find a published product for this address.",
                    OgImage = "/images/og/missing.png",
                    RobotsIndex = false,
                    RobotsFollow = false,
                },
                Settings = new SeoSettingsDto
                {
                    SiteName = "BlazorShop",
                    DefaultTitleSuffix = "| BlazorShop",
                    DefaultMetaDescription = "Shop the latest catalog.",
                    DefaultOgImage = "https://cdn.example.com/default-og.png",
                    BaseCanonicalUrl = "https://shop.example.com",
                },
            };

            var result = this._builder.Build(request);

            Assert.Equal("Missing Product | BlazorShop", result.Title);
            Assert.Equal("We couldn't find a published product for this address.", result.MetaDescription);
            Assert.Null(result.CanonicalUrl);
            Assert.Null(result.OgTitle);
            Assert.Null(result.OgDescription);
            Assert.Null(result.OgImage);
            Assert.Null(result.SiteName);
            Assert.False(result.RobotsIndex);
            Assert.False(result.RobotsFollow);
        }

        [Fact]
        public void Build_WhenTitleAlreadyContainsSuffix_DoesNotDuplicateSuffix()
        {
            var request = new SeoMetadataBuildRequest
            {
                PageTitle = "Running Shoes | BlazorShop",
                RelativePath = "/product/running-shoes",
                Settings = new SeoSettingsDto
                {
                    DefaultTitleSuffix = "| BlazorShop",
                    BaseCanonicalUrl = "https://shop.example.com",
                },
            };

            var result = this._builder.Build(request);

            Assert.Equal("Running Shoes | BlazorShop", result.Title);
        }

        [Fact]
        public void Build_WhenCanonicalOverrideIsUnsafe_FallsBackToRelativePathCanonical()
        {
            var request = new SeoMetadataBuildRequest
            {
                PageTitle = "Running Shoes",
                RelativePath = "/product/running-shoes",
                PageSeo = new SeoFieldsDto
                {
                    CanonicalUrl = "javascript:alert(1)",
                },
                Settings = new SeoSettingsDto
                {
                    BaseCanonicalUrl = "https://shop.example.com",
                },
            };

            var result = this._builder.Build(request);

            Assert.Equal("https://shop.example.com/product/running-shoes", result.CanonicalUrl);
        }

        [Fact]
        public void Build_WhenOpenGraphImageIsUnsafe_OmitsOpenGraphImage()
        {
            var request = new SeoMetadataBuildRequest
            {
                PageTitle = "Running Shoes",
                RelativePath = "/product/running-shoes",
                PageSeo = new SeoFieldsDto
                {
                    OgImage = "data:text/html;base64,PHNjcmlwdD4=",
                },
                Settings = new SeoSettingsDto
                {
                    BaseCanonicalUrl = "https://shop.example.com",
                },
            };

            var result = this._builder.Build(request);

            Assert.Null(result.OgImage);
        }
    }
}
