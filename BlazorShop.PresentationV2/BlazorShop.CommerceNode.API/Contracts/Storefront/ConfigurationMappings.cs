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
        public static StorefrontCaptchaConfigurationResponse ToStorefrontContract(CaptchaOptions options)
        {
            var enabledTargets = new List<string>();
            if (options.Enabled && options.Targets.Login)
            {
                enabledTargets.Add(CaptchaTargetNames.Login);
            }

            if (options.Enabled && options.Targets.Registration)
            {
                enabledTargets.Add(CaptchaTargetNames.Registration);
            }

            if (options.Enabled && options.Targets.Newsletter)
            {
                enabledTargets.Add(CaptchaTargetNames.Newsletter);
            }

            if (options.Enabled && options.Targets.PasswordRecovery)
            {
                enabledTargets.Add(CaptchaTargetNames.PasswordRecovery);
            }

            return new StorefrontCaptchaConfigurationResponse(
                options.Enabled,
                string.IsNullOrWhiteSpace(options.ProviderSystemName) ? "none" : options.ProviderSystemName,
                options.Enabled ? options.PublicSiteKey : null,
                enabledTargets,
                enabledTargets.ToDictionary(target => target, target => target, StringComparer.Ordinal));
        }
    }
}
