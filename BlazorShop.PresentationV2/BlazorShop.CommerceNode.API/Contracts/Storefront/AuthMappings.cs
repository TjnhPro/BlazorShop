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
        public static CreateUser ToApplicationRequest(this StorefrontRegisterRequest request)
        {
            return new CreateUser
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                CaptchaToken = request.CaptchaToken,
            };
        }
        public static LoginUser ToApplicationRequest(this StorefrontLoginRequest request)
        {
            return new LoginUser
            {
                Email = request.Email,
                Password = request.Password,
                CaptchaToken = request.CaptchaToken,
            };
        }
        public static ChangePassword ToApplicationRequest(this StorefrontChangePasswordRequest request)
        {
            return new ChangePassword
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword,
            };
        }
        public static ResetPassword ToApplicationRequest(this StorefrontResetPasswordRequest request)
        {
            return new ResetPassword
            {
                Email = request.Email,
                Token = request.Token,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
            };
        }
        public static UpdateProfile ToApplicationRequest(this StorefrontUpdateProfileRequest request)
        {
            return new UpdateProfile
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
            };
        }
        public static StorefrontTokenResponse ToStorefrontTokenContract(this LoginResponse response)
        {
            return new StorefrontTokenResponse(response.Token, ResolveAccessTokenExpiration(response.Token));
        }
        private static DateTime ResolveAccessTokenExpiration(string token)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.ValidTo == DateTime.MinValue
                    ? DateTime.UtcNow.AddHours(2)
                    : jwt.ValidTo;
            }
            catch (ArgumentException)
            {
                return DateTime.UtcNow.AddHours(2);
            }
        }
    }
}
