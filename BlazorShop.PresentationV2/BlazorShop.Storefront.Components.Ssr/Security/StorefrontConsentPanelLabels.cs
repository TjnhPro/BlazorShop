namespace BlazorShop.Storefront.Components.Ssr.Security;

public sealed record StorefrontConsentPanelLabels(
    string AriaLabel,
    string Heading,
    string Description,
    string PolicyLink,
    string Preferences,
    string Analytics,
    string Marketing,
    string EssentialOnly,
    string Revoke,
    string SaveChoices,
    string AcceptAll)
{
    public static StorefrontConsentPanelLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
