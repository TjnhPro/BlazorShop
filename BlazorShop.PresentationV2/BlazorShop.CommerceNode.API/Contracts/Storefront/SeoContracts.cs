namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed record StorefrontSeoDefaultsResponse(
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
