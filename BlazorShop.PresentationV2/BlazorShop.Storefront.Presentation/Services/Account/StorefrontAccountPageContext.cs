namespace BlazorShop.Storefront.Presentation.Services.Account
{
    public sealed record StorefrontAccountPageContext(
        string? Path,
        int Page,
        string? Error,
        string? Saved,
        string? AntiforgeryFieldName,
        string? AntiforgeryRequestToken);
}
