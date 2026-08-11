namespace BlazorShop.Storefront.V2.WASM.Components.Account;

public sealed record AccountNavigationClasses
{
    public static AccountNavigationClasses Empty { get; } = new();

    public string Nav { get; init; } = string.Empty;

    public string ActiveLink { get; init; } = string.Empty;

    public string InactiveLink { get; init; } = string.Empty;
}

public sealed record StorefrontAccountShellClasses
{
    public static StorefrontAccountShellClasses Empty { get; } = new();

    public string Section { get; init; } = string.Empty;
    public string Layout { get; init; } = string.Empty;
    public string ContentArticle { get; init; } = string.Empty;
    public string Header { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string UnknownAlert { get; init; } = string.Empty;
}
