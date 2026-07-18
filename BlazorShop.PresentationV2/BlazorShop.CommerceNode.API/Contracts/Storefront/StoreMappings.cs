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
        public static StorefrontMaintenanceResponse ToStorefrontMaintenanceContract(this CommerceCurrentStore store)
        {
            return new StorefrontMaintenanceResponse(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.MaintenanceModeEnabled,
                store.MaintenanceMessage);
        }
        public static StorefrontCurrentStoreResponse ToStorefrontContract(this CommerceCurrentStore store)
        {
            return new StorefrontCurrentStoreResponse(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.BaseUrl,
                store.PrimaryDomain,
                store.ForceHttps,
                store.CdnHost,
                store.LogoUrl,
                store.CompanyName,
                store.CompanyEmail,
                store.CompanyPhone,
                store.CompanyAddress,
                store.FaviconUrl,
                store.PngIconUrl,
                store.AppleTouchIconUrl,
                store.MsTileImageUrl,
                store.MsTileColor,
                store.DefaultCurrencyCode,
                store.DefaultCulture,
                store.SupportEmail,
                store.SupportPhone,
                store.MaintenanceModeEnabled,
                store.MaintenanceMessage,
                store.HtmlBodyId);
        }
    }
}
