namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;
    public static partial class StorefrontContractMappings
    {
        public static StorefrontConsentConfigurationResponse ToStorefrontContract(StorefrontConsentOptions options)
        {
            var optionalDefault = options.OptionalCategoriesDefaultEnabled;
            return new StorefrontConsentConfigurationResponse(
                options.Enabled,
                options.BannerRequired,
                string.IsNullOrWhiteSpace(options.CurrentVersion) ? "default" : options.CurrentVersion,
                string.IsNullOrWhiteSpace(options.PolicyPagePath) ? "/pages/cookies" : options.PolicyPagePath,
                [
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Essential, Required: true, DefaultEnabled: true),
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Preferences, Required: false, DefaultEnabled: optionalDefault),
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Analytics, Required: false, DefaultEnabled: optionalDefault),
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Marketing, Required: false, DefaultEnabled: optionalDefault),
                ],
                Math.Clamp(options.VisitorCookieLifetimeDays, 1, 3650));
        }
        public static StorefrontConsentResponse ToStorefrontContract(this StorefrontConsentSnapshot snapshot)
        {
            return new StorefrontConsentResponse(
                snapshot.Enabled,
                snapshot.BannerRequired,
                snapshot.ConsentVersion,
                snapshot.ConsentKey,
                new StorefrontConsentCategorySelectionResponse(
                    snapshot.Categories.Essential,
                    snapshot.Categories.Preferences,
                    snapshot.Categories.Analytics,
                    snapshot.Categories.Marketing),
                snapshot.UpdatedAtUtc,
                snapshot.RevokedAtUtc,
                snapshot.ExpiresAtUtc);
        }
    }
}
