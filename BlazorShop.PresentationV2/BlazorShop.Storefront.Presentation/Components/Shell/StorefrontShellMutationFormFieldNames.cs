namespace BlazorShop.Storefront.Presentation.Components.Shell;

internal static class StorefrontShellMutationFormFieldNames
{
    public static class CurrencyPreference
    {
        public const string CurrencyCode = nameof(global::BlazorShop.Storefront.Presentation.Endpoints.StorefrontCurrencyPreferenceForm.CurrencyCode);
        public const string ReturnUrl = nameof(global::BlazorShop.Storefront.Presentation.Endpoints.StorefrontCurrencyPreferenceForm.ReturnUrl);
    }

    public static class Logout
    {
        public const string ReturnUrl = nameof(global::BlazorShop.Storefront.Presentation.Services.StorefrontLogoutForm.ReturnUrl);
    }
}
