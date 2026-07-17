namespace BlazorShop.Storefront.Services
{
    public sealed class StorefrontChangePasswordForm
    {
        public string? CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}
