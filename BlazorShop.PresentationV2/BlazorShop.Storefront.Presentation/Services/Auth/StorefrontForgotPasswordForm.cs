namespace BlazorShop.Storefront.Presentation.Services
{
    public sealed class StorefrontForgotPasswordForm
    {
        public string? Email { get; set; }

        public string? CaptchaToken { get; set; }
    }
}
