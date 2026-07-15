namespace BlazorShop.Storefront.Configuration
{
    using System.Net;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Options;

    public sealed class StorefrontForwardedHeadersOptionsSetup : IConfigureOptions<ForwardedHeadersOptions>
    {
        public const string SectionName = "Storefront:ForwardedHeaders";

        private readonly IConfiguration _configuration;

        public StorefrontForwardedHeadersOptionsSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(ForwardedHeadersOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                       | ForwardedHeaders.XForwardedProto
                                       | ForwardedHeaders.XForwardedHost;

            foreach (var proxy in _configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            foreach (var network in _configuration.GetSection($"{SectionName}:KnownNetworks").Get<string[]>() ?? [])
            {
                if (TryParseKnownNetwork(network, out var knownNetwork))
                {
                    options.KnownIPNetworks.Add(knownNetwork);
                }
            }
        }

        private static bool TryParseKnownNetwork(string? value, out System.Net.IPNetwork network)
        {
            network = default!;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return System.Net.IPNetwork.TryParse(value, out network);
        }
    }
}
