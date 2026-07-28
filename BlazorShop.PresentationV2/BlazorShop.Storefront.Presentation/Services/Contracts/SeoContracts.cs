namespace BlazorShop.Storefront.Presentation.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Presentation.Options;

    using Microsoft.Extensions.Options;


    public sealed record StorefrontSeoDefaults(
        string? SiteName,
        string? DefaultTitleSuffix,
        string? DefaultMetaDescription,
        string? DefaultOgImage,
        string? BaseCanonicalUrl,
        string? CompanyName,
        string? CompanyLogoUrl,
        string? CompanyPhone,
        string? CompanyEmail,
        string? CompanyAddress,
        string? FacebookUrl,
        string? InstagramUrl,
        string? XUrl);
}
