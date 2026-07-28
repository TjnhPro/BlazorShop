namespace BlazorShop.Storefront.Presentation.Configuration;

using BlazorShop.Storefront.Presentation.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class StorefrontPublicUrlOptionsValidator : IValidateOptions<StorefrontPublicUrlOptions>
{
    private readonly IHostEnvironment hostEnvironment;

    public StorefrontPublicUrlOptionsValidator(IHostEnvironment hostEnvironment)
    {
        this.hostEnvironment = hostEnvironment;
    }

    public ValidateOptionsResult Validate(string? name, StorefrontPublicUrlOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.BaseUrl) && !IsAbsoluteHttpUrl(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("PublicUrl:BaseUrl must be an absolute http or https URL when configured.");
        }

        if (this.hostEnvironment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        return string.IsNullOrWhiteSpace(options.BaseUrl)
            ? ValidateOptionsResult.Fail("PublicUrl:BaseUrl is required outside Development so canonical and discovery URLs do not depend on request-host inference.")
            : ValidateOptionsResult.Success;
    }

    private static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
