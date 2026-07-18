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
        public static BlazorShop.Application.CommerceNode.Customers.StorefrontCustomerProfileUpdateRequest ToApplicationRequest(
            this StorefrontCustomerProfileUpdateRequest request,
            Guid storeId,
            string appUserId)
        {
            return new BlazorShop.Application.CommerceNode.Customers.StorefrontCustomerProfileUpdateRequest(
                storeId,
                appUserId,
                request.Email,
                request.FullName,
                request.FirstName,
                request.LastName,
                request.Company,
                request.PhoneNumber,
                request.PreferredLanguage,
                request.PreferredCurrencyCode);
        }
        public static StorefrontCustomerProfileResponse ToStorefrontContract(
            this BlazorShop.Application.CommerceNode.Customers.StorefrontCustomerProfile profile)
        {
            return new StorefrontCustomerProfileResponse(
                profile.Id,
                profile.Email,
                profile.FullName,
                profile.FirstName,
                profile.LastName,
                profile.Company,
                profile.Phone,
                profile.PreferredLanguage,
                profile.PreferredCurrencyCode,
                profile.CreatedAt,
                profile.LastActivityAtUtc);
        }
    }
}
