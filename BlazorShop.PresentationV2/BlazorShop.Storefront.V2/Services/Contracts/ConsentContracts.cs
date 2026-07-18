namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models.Discovery;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;

    public sealed record StorefrontConsentConfiguration(
        bool Enabled,
        bool BannerRequired,
        string CurrentVersion,
        string PolicyPagePath,
        IReadOnlyList<StorefrontConsentCategory> Categories,
        int VisitorCookieLifetimeDays);

    public sealed record StorefrontConsentCategory(
        string Name,
        bool Required,
        bool DefaultEnabled);

    public sealed record StorefrontConsentState(
        bool Enabled,
        bool BannerRequired,
        string ConsentVersion,
        string? ConsentKey,
        StorefrontConsentCategorySelection Categories,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    public sealed record StorefrontConsentCategorySelection(
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
