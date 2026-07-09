namespace BlazorShop.Storefront.Services
{
    public sealed class StorefrontLoginForm
    {
        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
