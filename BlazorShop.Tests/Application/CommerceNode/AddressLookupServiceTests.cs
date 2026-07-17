namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.DTOs;

    using Xunit;

    public sealed class AddressLookupServiceTests
    {
        private readonly AddressLookupService service = new();

        [Fact]
        public async Task GetCountriesAsync_ReturnsDeterministicPublicCatalog()
        {
            var countries = await this.service.GetCountriesAsync();

            Assert.Contains(countries, country => country.Code == "US" && country.StateProvinceRequired);
            Assert.Contains(countries, country => country.Code == "CA" && country.StateProvinceRequired);
            Assert.Contains(countries, country => country.Code == "AU" && country.StateProvinceRequired);
            Assert.Contains(countries, country => country.Code == "VN" && !country.StateProvinceRequired);
            Assert.Equal(countries.OrderBy(country => country.Code, StringComparer.Ordinal).Select(country => country.Code), countries.Select(country => country.Code));
        }

        [Fact]
        public async Task GetStatesAsync_WhenCountryHasStates_ReturnsStates()
        {
            var result = await this.service.GetStatesAsync(" us ");

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.NotNull(result.Payload);
            Assert.Contains(result.Payload!, state => state.Code == "NY" && state.Name == "New York");
        }

        [Fact]
        public async Task GetStatesAsync_WhenKnownCountryHasNoStates_ReturnsEmptyList()
        {
            var result = await this.service.GetStatesAsync("vn");

            Assert.True(result.Success);
            Assert.Empty(result.Payload!);
        }

        [Fact]
        public async Task GetStatesAsync_WhenCountryIsUnknown_ReturnsNotFound()
        {
            var result = await this.service.GetStatesAsync("zz");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Empty(result.Payload!);
        }

        [Fact]
        public async Task GetConfigurationAsync_ReturnsValidationAlignedLimits()
        {
            var configuration = await this.service.GetConfigurationAsync();

            Assert.True(configuration.CompanyEnabled);
            Assert.True(configuration.PhoneEnabled);
            Assert.False(configuration.PhoneRequired);
            Assert.True(configuration.PostalCodeRequired);
            Assert.False(configuration.BillingAddressEnabled);
            Assert.True(configuration.UseShippingAddressAsBillingDefault);
            Assert.Equal(120, configuration.FirstNameMaxLength);
            Assert.Equal(120, configuration.LastNameMaxLength);
            Assert.Equal(160, configuration.CompanyMaxLength);
            Assert.Equal(240, configuration.AddressLineMaxLength);
            Assert.Equal(32, configuration.PostalCodeMaxLength);
            Assert.Equal(32, configuration.PhoneMaxLength);
            Assert.Equal(256, configuration.EmailMaxLength);
            Assert.Equal(["AU", "CA", "US"], configuration.StateProvinceRequiredCountryCodes);
        }
    }
}
