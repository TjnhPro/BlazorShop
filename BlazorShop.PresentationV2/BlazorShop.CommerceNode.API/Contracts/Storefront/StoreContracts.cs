namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed record StorefrontMaintenanceResponse(
        Guid PublicId,
        string StoreKey,
        string Name,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontCurrentStoreResponse(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps,
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string DefaultCurrencyCode,
        string DefaultCulture,
        string? SupportEmail,
        string? SupportPhone,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage,
        string? HtmlBodyId);

    public sealed record StorefrontStoreIdentityResponse(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps);

    public sealed record StorefrontBrandingResponse(
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string? SupportEmail,
        string? SupportPhone,
        string? HtmlBodyId);
}
