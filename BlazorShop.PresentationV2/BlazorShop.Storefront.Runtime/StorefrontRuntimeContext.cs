namespace BlazorShop.Storefront.Runtime
{
    using Microsoft.Extensions.Options;

    public interface IStorefrontRuntimeContext
    {
        string CommerceNodeBaseUrl { get; }

        string StoreKey { get; }

        string? PublicBaseUrl { get; }
    }

    internal sealed class OptionsStorefrontRuntimeContext : IStorefrontRuntimeContext
    {
        private readonly IOptions<StorefrontRuntimeOptions> options;

        public OptionsStorefrontRuntimeContext(IOptions<StorefrontRuntimeOptions> options)
        {
            this.options = options;
        }

        public string CommerceNodeBaseUrl => this.options.Value.CommerceNodeBaseUrl;

        public string StoreKey => this.options.Value.StoreKey;

        public string? PublicBaseUrl => this.options.Value.PublicBaseUrl;
    }
}
