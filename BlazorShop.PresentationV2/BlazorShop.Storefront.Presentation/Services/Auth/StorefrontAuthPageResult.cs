namespace BlazorShop.Storefront.Presentation.Services.Auth;

public sealed record StorefrontAuthPageResult(StorefrontAuthPageContext? Context, string? RedirectUrl)
{
    public bool ShouldRedirect => !string.IsNullOrWhiteSpace(RedirectUrl);
}
