extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontPriceFormatterTests
    {
        [Theory]
        [InlineData("en-US", "USD", "USD 1,234.50")]
        [InlineData("vi-VN", "vnd", "VND 1.234,50")]
        public void Format_UsesDisplayCultureAndCurrencyCode(string cultureName, string currencyCode, string expected)
        {
            var formatter = new StorefrontPriceFormatter();
            var context = StorefrontDisplayContext.Fallback with
            {
                CultureName = cultureName,
                CurrencyCode = currencyCode,
            };

            var formatted = formatter.Format(1234.5m, context);

            Assert.Equal(expected, formatted);
        }

        [Fact]
        public void Format_WhenContextIsInvalid_UsesStableFallbacks()
        {
            var formatter = new StorefrontPriceFormatter();
            var context = StorefrontDisplayContext.Fallback with
            {
                CultureName = "invalid-culture",
                CurrencyCode = "USDO",
            };

            var formatted = formatter.Format(10m, context);

            Assert.Equal("USD 10.00", formatted);
        }
    }
}
