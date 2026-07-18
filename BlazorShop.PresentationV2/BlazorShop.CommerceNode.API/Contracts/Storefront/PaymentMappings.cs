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
        public static StorefrontPaymentAttemptResponse ToStorefrontContract(this PaymentAttemptDto result)
        {
            var nextAction = string.IsNullOrWhiteSpace(result.NextActionType)
                ? null
                : new StorefrontPaymentNextActionResponse(result.NextActionType, result.NextActionUrl);

            return new StorefrontPaymentAttemptResponse(
                result.Id,
                result.CheckoutSessionId,
                result.OrderId,
                result.PaymentMethodKey,
                result.ProviderKey,
                result.State,
                result.Amount,
                result.CurrencyCode,
                result.ProviderReference,
                result.ProviderSessionId,
                nextAction,
                result.FailureCode,
                result.FailureMessage,
                result.ExpiresAtUtc,
                result.CreatedAtUtc,
                result.UpdatedAtUtc);
        }
        public static StorefrontPaymentMethodResponse ToStorefrontContract(this GetPaymentMethod paymentMethod)
        {
            return new StorefrontPaymentMethodResponse(
                paymentMethod.Id,
                paymentMethod.Key,
                paymentMethod.Name,
                paymentMethod.Description,
                paymentMethod.ShortDisplayText,
                paymentMethod.IconUrl,
                paymentMethod.SupportedCurrencyCodes,
                paymentMethod.SupportedCountryCodes);
        }
    }
}
