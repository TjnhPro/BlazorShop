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
        public static StorefrontPagedResponse<TTarget> ToStorefrontContract<TSource, TTarget>(
            this PagedResult<TSource> page,
            Func<TSource, TTarget> map)
        {
            return new StorefrontPagedResponse<TTarget>(
                page.Items.Select(map).ToArray(),
                page.PageNumber,
                page.PageSize,
                page.TotalCount,
                page.TotalPages);
        }
    }
}
