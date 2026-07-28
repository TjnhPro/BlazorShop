namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Models;
    using BlazorShop.Storefront.Presentation.Services;

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
        StorefrontConsentContext Consent,
        StorefrontLinkContext Links,
        string ReturnUrl);

    public sealed record StorefrontHeaderContext(
        StorefrontBrandContext Brand,
        StorefrontNavigationContext Navigation,
        StorefrontSearchContext Search,
        StorefrontCurrencyContext Currency,
        StorefrontAccountMenuContext AccountMenu,
        StorefrontLinkContext Links,
        string ReturnUrl);

    public sealed record StorefrontFooterContext(
        StorefrontBrandContext Brand,
        StorefrontNavigationContext Navigation,
        StorefrontLinkContext Links,
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

    public sealed record StorefrontLinkContext(
        StorefrontShellLink Home,
        StorefrontShellLink Search,
        StorefrontShellLink Cart,
        StorefrontShellLink Checkout,
        StorefrontShellLink AccountRoot,
        StorefrontShellLink AccountProfile,
        StorefrontShellLink AccountAddresses,
        StorefrontShellLink AccountOrders,
        StorefrontShellLink AccountPassword,
        StorefrontShellLink SignIn,
        StorefrontShellLink Register,
        StorefrontShellLink LogoutFormTarget,
        StorefrontShellLink NewReleases,
        StorefrontShellLink TodaysDeals,
        StorefrontShellLink CustomerService,
        StorefrontShellLink Privacy,
        StorefrontShellLink Terms)
    {
        public static StorefrontLinkContext Default { get; } = Create();

        public string Category(string? slug) => StorefrontRoutes.Category(slug);

        public string Product(string? slug) => StorefrontRoutes.Product(slug);

        public string Page(string? slug) => StorefrontRoutes.Page(slug);

        public string CategoryUrl(
            string? slug,
            int? pageNumber = null,
            int? pageSize = null,
            ProductCatalogSortBy? sortBy = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null)
        {
            return StorefrontRoutes.CategoryUrl(slug, pageNumber, pageSize, sortBy, minPrice, maxPrice, inStock);
        }

        public string SearchUrl(
            string? query,
            string? categorySlug = null,
            int? pageNumber = null,
            int? pageSize = null,
            ProductCatalogSortBy? sortBy = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null)
        {
            return StorefrontRoutes.SearchUrl(query, categorySlug, pageNumber, pageSize, sortBy, minPrice, maxPrice, inStock);
        }

        internal static StorefrontLinkContext Create()
        {
            return new StorefrontLinkContext(
                new("Home", StorefrontRoutes.Home),
                new("Search", StorefrontRoutes.Search),
                new("Cart", StorefrontRoutes.Cart),
                new("Checkout", StorefrontRoutes.Checkout),
                new("Account", StorefrontRoutes.Account),
                new("Profile", StorefrontRoutes.AccountProfile),
                new("Addresses", StorefrontRoutes.AccountAddresses),
                new("Orders", StorefrontRoutes.AccountOrders),
                new("Password", StorefrontRoutes.AccountChangePassword),
                new("Sign in", StorefrontRoutes.SignIn),
                new("Register", StorefrontRoutes.Register),
                new("Logout", StorefrontRoutes.Logout),
                new("New Releases", StorefrontRoutes.NewReleases),
                new("Today's Deals", StorefrontRoutes.TodaysDeals),
                new("Customer Service", StorefrontRoutes.CustomerService),
                new("Privacy", StorefrontRoutes.Privacy),
                new("Terms", StorefrontRoutes.Terms));
        }
    }

    public sealed record StorefrontShellLink(
        string Label,
        string Href,
        string? Target = null,
        int DisplayOrder = 0);

    public sealed record StorefrontShellMenu(
        string SystemName,
        IReadOnlyList<StorefrontShellLink> Links);
}
