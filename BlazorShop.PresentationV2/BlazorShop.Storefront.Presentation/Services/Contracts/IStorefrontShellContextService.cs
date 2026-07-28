namespace BlazorShop.Storefront.Services.Contracts
{
    public interface IStorefrontShellContextService
    {
        Task<StorefrontShellContext> GetAsync(CancellationToken cancellationToken = default);
    }

    public sealed record StorefrontShellContext(
        StorefrontDisplayContext Display,
        StorefrontBrandContext Brand,
        StorefrontHeaderContext Header,
        StorefrontFooterContext Footer,
        StorefrontAccountMenuContext AccountMenu,
        StorefrontNavigationContext Navigation,
        StorefrontSearchContext Search,
        StorefrontCurrencyContext Currency,
        string ReturnUrl);

    public sealed record StorefrontHeaderContext(
        StorefrontBrandContext Brand,
        StorefrontNavigationContext Navigation,
        StorefrontSearchContext Search,
        StorefrontCurrencyContext Currency,
        StorefrontAccountMenuContext AccountMenu,
        string ReturnUrl);

    public sealed record StorefrontFooterContext(
        StorefrontBrandContext Brand,
        StorefrontNavigationContext Navigation,
        string CopyrightLabel,
        string? ContactEmail,
        string? ContactPhone,
        string? ContactPhoneHref,
        string? CompanyAddress);

    public sealed record StorefrontAccountMenuContext(
        bool IsAuthenticated,
        bool IsAdmin,
        string? DisplayName,
        string? Email,
        IReadOnlyList<StorefrontShellLink> Links,
        string LogoutReturnUrl);

    public sealed record StorefrontNavigationContext(
        StorefrontShellMenu? MainMenu,
        StorefrontShellMenu? FooterCompanyMenu,
        StorefrontShellMenu? FooterSupportMenu,
        StorefrontShellMenu? FooterLegalMenu,
        IReadOnlyList<StorefrontShellLink> HeaderLinks,
        IReadOnlyList<StorefrontShellLink> FooterCompanyLinks,
        IReadOnlyList<StorefrontShellLink> FooterSupportLinks,
        IReadOnlyList<StorefrontShellLink> FooterLegalLinks);

    public sealed record StorefrontSearchContext(
        string Action,
        IReadOnlyList<StorefrontShellLink> Categories);

    public sealed record StorefrontCurrencyContext(
        string CurrentCurrencyCode,
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes,
        bool ShowSelector,
        string PreferenceAction,
        string ReturnUrl);

    public sealed record StorefrontBrandContext(
        string Name,
        string Label,
        string HomeLabel,
        string? LogoUrl);

    public sealed record StorefrontShellLink(
        string Label,
        string Href,
        string? Target = null,
        int DisplayOrder = 0);

    public sealed record StorefrontShellMenu(
        string SystemName,
        IReadOnlyList<StorefrontShellLink> Links);
}
