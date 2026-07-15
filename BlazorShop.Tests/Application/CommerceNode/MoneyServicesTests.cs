namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;

    using Xunit;

    public sealed class MoneyServicesTests
    {
        [Theory]
        [InlineData(12.345, "USD", 12.35)]
        [InlineData(12.5, "JPY", 13)]
        public void RoundPaymentAmount_UsesCurrencyDecimalDigits(decimal amount, string currencyCode, decimal expected)
        {
            var service = new MoneyRoundingService(new CurrencyMetadataService());

            var rounded = service.RoundPaymentAmount(amount, currencyCode);

            Assert.Equal(expected, rounded);
        }

        [Theory]
        [InlineData(12.34, "USD", 1234)]
        [InlineData(12.34, "JPY", 12)]
        [InlineData(12.5, "JPY", 13)]
        public void ToMinorUnits_UsesCurrencyDecimalDigits(decimal amount, string currencyCode, long expected)
        {
            var metadata = new CurrencyMetadataService();
            var converter = new PaymentMinorUnitConverter(
                metadata,
                new MoneyRoundingService(metadata));

            var minorUnits = converter.ToMinorUnits(amount, currencyCode);

            Assert.Equal(expected, minorUnits);
        }
    }
}
