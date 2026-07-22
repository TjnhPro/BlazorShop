extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Configuration;

    public sealed class StorefrontV2ForwardedHeadersOptionsTests
    {
        [Fact]
        public void Configure_EnablesForwardedForProtoAndHost()
        {
            var setup = new StorefrontForwardedHeadersOptionsSetup(CreateConfiguration());
            var options = new ForwardedHeadersOptions();

            setup.Configure(options);

            Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
            Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
            Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
        }

        [Fact]
        public void Configure_AddsConfiguredKnownProxiesAndNetworks()
        {
            var setup = new StorefrontForwardedHeadersOptionsSetup(CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Storefront:ForwardedHeaders:KnownProxies:0"] = "10.0.0.10",
                    ["Storefront:ForwardedHeaders:KnownNetworks:0"] = "10.20.0.0/16",
                }));
            var options = new ForwardedHeadersOptions();
            var originalNetworkCount = options.KnownIPNetworks.Count;

            setup.Configure(options);

            Assert.Contains(IPAddress.Parse("10.0.0.10"), options.KnownProxies);
            Assert.True(options.KnownIPNetworks.Count > originalNetworkCount);
        }

        [Fact]
        public void Configure_IgnoresInvalidProxyAndNetworkEntries()
        {
            var setup = new StorefrontForwardedHeadersOptionsSetup(CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Storefront:ForwardedHeaders:KnownProxies:0"] = "not-an-ip",
                    ["Storefront:ForwardedHeaders:KnownNetworks:0"] = "not-a-cidr",
                }));
            var options = new ForwardedHeadersOptions();
            var originalProxyCount = options.KnownProxies.Count;
            var originalNetworkCount = options.KnownIPNetworks.Count;

            setup.Configure(options);

            Assert.Equal(originalProxyCount, options.KnownProxies.Count);
            Assert.Equal(originalNetworkCount, options.KnownIPNetworks.Count);
        }

        private static IConfiguration CreateConfiguration(Dictionary<string, string?>? values = null)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values ?? [])
                .Build();
        }
    }
}
