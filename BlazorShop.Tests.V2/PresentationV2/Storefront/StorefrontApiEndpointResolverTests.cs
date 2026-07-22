extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Microsoft.Extensions.Configuration;
    using StorefrontV2::BlazorShop.Storefront.Configuration;
    using Xunit;

    public sealed class StorefrontApiEndpointResolverTests
    {
        [Theory]
        [InlineData("Api:StoreKey")]
        [InlineData("StoreKey")]
        [InlineData("STORE_KEY")]
        public void ResolveScopedStorefrontApiBaseAddress_UsesConfiguredStoreKey(string keyName)
        {
            var configuration = BuildConfiguration(
                ("Api:BaseUrl", "https://commerce.example/api/"),
                (keyName, "store-alpha"));

            var uri = StorefrontApiEndpointResolver.ResolveScopedStorefrontApiBaseAddress(configuration);

            Assert.Equal("https://commerce.example/api/storefront/stores/store-alpha/", uri.ToString());
        }

        [Fact]
        public void ResolveScopedStorefrontApiBaseAddress_UsesUnscopedApiBaseWhenStoreKeyMissing()
        {
            var configuration = BuildConfiguration(("Api:BaseUrl", "https://commerce.example/api/"));

            var uri = StorefrontApiEndpointResolver.ResolveScopedStorefrontApiBaseAddress(configuration);

            Assert.Equal("https://commerce.example/api/", uri.ToString());
        }

        [Fact]
        public void ResolveCommerceNodeBaseAddress_RemovesApiPathForMediaProxy()
        {
            var configuration = BuildConfiguration(
                ("Api:BaseUrl", "https://commerce.example/api/"),
                ("Api:StoreKey", "store-alpha"));

            var uri = StorefrontApiEndpointResolver.ResolveCommerceNodeBaseAddress(configuration);

            Assert.Equal("https://commerce.example/", uri.ToString());
        }

        private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
                .Build();
        }
    }
}
