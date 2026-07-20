namespace BlazorShop.CommerceNode.API.Configuration
{
    using System.Net;

    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Options;

    public sealed class CommerceNodeForwardedHeadersOptionsSetup : IConfigureOptions<ForwardedHeadersOptions>
    {
        public const string SectionName = "Runtime:ForwardedHeaders";

        private readonly IConfiguration configuration;

        public CommerceNodeForwardedHeadersOptionsSetup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Configure(ForwardedHeadersOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                       | ForwardedHeaders.XForwardedProto
                                       | ForwardedHeaders.XForwardedHost;

            var forwardLimit = this.configuration.GetValue<int?>($"{SectionName}:ForwardLimit");
            if (forwardLimit is not null)
            {
                options.ForwardLimit = Math.Clamp(forwardLimit.Value, 1, 10);
            }

            foreach (var proxy in this.configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            foreach (var network in this.configuration.GetSection($"{SectionName}:KnownNetworks").Get<string[]>() ?? [])
            {
                if (System.Net.IPNetwork.TryParse(network, out var knownNetwork))
                {
                    options.KnownIPNetworks.Add(knownNetwork);
                }
            }
        }
    }
}
