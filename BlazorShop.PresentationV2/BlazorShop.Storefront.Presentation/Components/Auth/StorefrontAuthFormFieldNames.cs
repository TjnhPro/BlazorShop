namespace BlazorShop.Storefront.Presentation.Components.Auth;

internal static class StorefrontAuthFormFieldNames
{
    public static class SignIn
    {
        public const string Email = nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.Email);
        public const string Password = nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.Password);
        public const string CaptchaToken = nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.CaptchaToken);
        public const string ReturnUrl = nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.ReturnUrl);
    }

    public static class Register
    {
        public const string FullName = nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.FullName);
        public const string Email = nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.Email);
        public const string Password = nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.Password);
        public const string ConfirmPassword = nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.ConfirmPassword);
        public const string CaptchaToken = nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.CaptchaToken);
        public const string ReturnUrl = nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.ReturnUrl);
    }

    public static class ForgotPassword
    {
        public const string Email = nameof(global::BlazorShop.Storefront.Services.StorefrontForgotPasswordForm.Email);
        public const string CaptchaToken = nameof(global::BlazorShop.Storefront.Services.StorefrontForgotPasswordForm.CaptchaToken);
    }

    public static class ResetPassword
    {
        public const string Email = nameof(global::BlazorShop.Storefront.Services.StorefrontResetPasswordForm.Email);
        public const string Token = nameof(global::BlazorShop.Storefront.Services.StorefrontResetPasswordForm.Token);
        public const string Password = nameof(global::BlazorShop.Storefront.Services.StorefrontResetPasswordForm.Password);
        public const string ConfirmPassword = nameof(global::BlazorShop.Storefront.Services.StorefrontResetPasswordForm.ConfirmPassword);
    }
}
