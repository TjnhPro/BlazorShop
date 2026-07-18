namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed record StorefrontConsentConfigurationResponse(
        bool Enabled,
        bool BannerRequired,
        string CurrentVersion,
        string PolicyPagePath,
        IReadOnlyList<StorefrontConsentCategoryResponse> Categories,
        int VisitorCookieLifetimeDays);

    public sealed record StorefrontConsentCategoryResponse(
        string Name,
        bool Required,
        bool DefaultEnabled);

    public sealed record StorefrontConsentResponse(
        bool Enabled,
        bool BannerRequired,
        string ConsentVersion,
        string? ConsentKey,
        StorefrontConsentCategorySelectionResponse Categories,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    public sealed record StorefrontConsentCategorySelectionResponse(
        bool Essential,
        bool Preferences,
        bool Analytics,
        bool Marketing);

    public sealed class StorefrontConsentSaveRequest
    {
        public bool Preferences { get; set; }

        public bool Analytics { get; set; }

        public bool Marketing { get; set; }
    }
}
