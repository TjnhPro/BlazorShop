namespace BlazorShop.Storefront.Presentation.Routing;

public static class StorefrontRoutePatterns
{
    public const string Home = "/";
    public const string Category = "/category/{Slug}";
    public const string Product = "/product/{Slug}";
    public const string Search = "/search";
    public const string Deals = "/todays-deals";
    public const string NewReleases = "/new-releases";
    public const string Content = "/pages/{Slug}";
    public const string Cart = "/my-cart";
    public const string Checkout = "/checkout";
    public const string PaymentSuccess = "/payment-success";
    public const string PaymentCancel = "/payment-cancel";
    public const string PaymentResult = "/payment/result";
    public const string SignIn = "/signin";
    public const string Register = "/register";
    public const string ForgotPassword = "/forgot-password";
    public const string ResetPassword = "/reset-password";
    public const string Account = "/account";
    public const string AccountWildcard = "/account/{*Path}";
    public const string Maintenance = "/maintenance";
    public const string NotFound = "/{*Path:nonfile}";
}
