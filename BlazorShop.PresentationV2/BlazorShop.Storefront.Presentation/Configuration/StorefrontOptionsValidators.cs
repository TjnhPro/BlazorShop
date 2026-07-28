namespace BlazorShop.Storefront.Presentation.Configuration
{
    using BlazorShop.Storefront.Presentation.Options;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed class StorefrontApiOptionsValidator : IValidateOptions<StorefrontApiOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;

        public StorefrontApiOptionsValidator(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
        }

        public ValidateOptionsResult Validate(string? name, StorefrontApiOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl) && !IsAbsoluteHttpUrl(options.BaseUrl))
            {
                return ValidateOptionsResult.Fail("Api:BaseUrl must be an absolute http or https URL when configured.");
            }

            if (!string.IsNullOrWhiteSpace(options.StoreKey) && options.StoreKey.Trim().Length > 128)
            {
                return ValidateOptionsResult.Fail("Api:StoreKey must be at most 128 characters when configured.");
            }

            if (_hostEnvironment.IsDevelopment()
                || HasServiceDiscoveryEndpoint("apiservice")
                || HasRuntimeCommerceNodeBaseUrl())
            {
                return ValidateOptionsResult.Success;
            }

            return string.IsNullOrWhiteSpace(options.BaseUrl)
                ? ValidateOptionsResult.Fail("Api:BaseUrl is required outside Development when Services:apiservice:* is not configured.")
                : ValidateOptionsResult.Success;
        }

        private bool HasServiceDiscoveryEndpoint(string serviceName)
        {
            return IsAbsoluteHttpUrl(_configuration[$"Services:{serviceName}:https:0"])
                || IsAbsoluteHttpUrl(_configuration[$"Services:{serviceName}:http:0"]);
        }

        private bool HasRuntimeCommerceNodeBaseUrl()
        {
            return IsAbsoluteHttpUrl(_configuration[$"{StorefrontRuntimeBindingOptions.SectionName}:CommerceNodeBaseUrl"]);
        }

        private static bool IsAbsoluteHttpUrl(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public sealed class StorefrontClientAppOptionsValidator : IValidateOptions<ClientAppOptions>
    {
        private readonly IHostEnvironment _hostEnvironment;

        public StorefrontClientAppOptionsValidator(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public ValidateOptionsResult Validate(string? name, ClientAppOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl) && !IsAbsoluteHttpUrl(options.BaseUrl))
            {
                return ValidateOptionsResult.Fail("ClientApp:BaseUrl must be an absolute http or https URL when configured.");
            }

            if (_hostEnvironment.IsDevelopment())
            {
                return ValidateOptionsResult.Success;
            }

            return string.IsNullOrWhiteSpace(options.BaseUrl)
                ? ValidateOptionsResult.Fail("ClientApp:BaseUrl is required outside Development when checkout or account redirects need an external client app.")
                : ValidateOptionsResult.Success;
        }

        private static bool IsAbsoluteHttpUrl(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public sealed class StorefrontStoreResolutionOptionsValidator : IValidateOptions<StorefrontStoreResolutionOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;

        public StorefrontStoreResolutionOptionsValidator(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
        }

        public ValidateOptionsResult Validate(string? name, StorefrontStoreResolutionOptions options)
        {
            if (!StorefrontStoreResolutionOptions.IsCurrentStoreRequired(options, _hostEnvironment))
            {
                return ValidateOptionsResult.Success;
            }

            return string.IsNullOrWhiteSpace(StorefrontStoreKeyResolver.Resolve(_configuration))
                ? ValidateOptionsResult.Fail("Api:StoreKey, StoreKey, or STORE_KEY is required when StoreResolution:RequireCurrentStore is enabled.")
                : ValidateOptionsResult.Success;
        }
    }

    public sealed class StorefrontRuntimeBindingOptionsValidator : IValidateOptions<StorefrontRuntimeBindingOptions>
    {
        public ValidateOptionsResult Validate(string? name, StorefrontRuntimeBindingOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.CommerceNodeBaseUrl) && !IsAbsoluteHttpUrl(options.CommerceNodeBaseUrl))
            {
                return ValidateOptionsResult.Fail("Storefront:CommerceNodeBaseUrl must be an absolute http or https URL when configured.");
            }

            if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl) && !IsAbsoluteHttpUrl(options.PublicBaseUrl))
            {
                return ValidateOptionsResult.Fail("Storefront:PublicBaseUrl must be an absolute http or https URL when configured.");
            }

            if (!string.IsNullOrWhiteSpace(options.StoreKey) && options.StoreKey.Trim().Length > 128)
            {
                return ValidateOptionsResult.Fail("Storefront:StoreKey must be at most 128 characters when configured.");
            }

            return ValidateOptionsResult.Success;
        }

        private static bool IsAbsoluteHttpUrl(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
