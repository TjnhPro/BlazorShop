extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using BlazorShop.Web.SharedV2.Models.Pages;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontPagePresentationResolverTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown_template")]
        [InlineData("about")]
        public void Resolve_UsesStandardWebPageFallbackForStandardContent(string? pageKey)
        {
            var presentation = Resolve(pageKey);

            Assert.Equal(StorefrontPageLayoutKind.Standard, presentation.LayoutKind);
            Assert.Equal(StorefrontPageStructuredDataKind.WebPage, presentation.StructuredDataKind);
            Assert.Empty(presentation.FaqEntries);
        }

        [Theory]
        [InlineData("shipping_information")]
        [InlineData("payment_information")]
        [InlineData("return_refund_policy")]
        [InlineData("terms_conditions")]
        [InlineData("privacy_policy")]
        [InlineData("cookie_information")]
        public void Resolve_UsesPolicyLayoutForPolicyTemplates(string pageKey)
        {
            var presentation = Resolve(pageKey);

            Assert.Equal(pageKey, presentation.TemplateKey);
            Assert.Equal(StorefrontPageLayoutKind.Policy, presentation.LayoutKind);
            Assert.Equal(StorefrontPageStructuredDataKind.WebPage, presentation.StructuredDataKind);
        }

        [Fact]
        public void Resolve_UsesFaqLayoutButWebPageStructuredDataWhenNoStructuredEntriesExist()
        {
            var presentation = Resolve("faq");

            Assert.Equal(StorefrontPageLayoutKind.Faq, presentation.LayoutKind);
            Assert.Equal(StorefrontPageStructuredDataKind.WebPage, presentation.StructuredDataKind);
            Assert.Empty(presentation.FaqEntries);
        }

        [Fact]
        public void Resolve_UsesSupportLayoutForCustomerServiceWithoutContactFormBehavior()
        {
            var presentation = Resolve("customer_service");

            Assert.Equal(StorefrontPageLayoutKind.Support, presentation.LayoutKind);
            Assert.Equal(StorefrontPageStructuredDataKind.WebPage, presentation.StructuredDataKind);
            Assert.DoesNotContain("contact", presentation.ArticleClass, StringComparison.OrdinalIgnoreCase);
        }

        private static StorefrontPagePresentation Resolve(string? pageKey)
        {
            return new StorefrontPagePresentationResolver().Resolve(new GetStorefrontPage
            {
                Slug = "test",
                Title = "Test",
                BodyHtml = "<p>Test</p>",
                PageKey = pageKey,
            });
        }
    }
}
