namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.AspNetCore.Http;

    public sealed class StorefrontShellContextService : IStorefrontShellContextService
    {
        private static readonly object CacheKey = new();

        private readonly IStorefrontDisplayContextProvider _displayContextProvider;
        private readonly IStorefrontCatalogClient _catalogClient;
        private readonly IStorefrontPageNavigationProvider _pageNavigationProvider;
        private readonly IStorefrontNavigationProvider _navigationProvider;
        private readonly IStorefrontSessionResolver _sessionResolver;
        private readonly IStorefrontClientAppUrlResolver _clientAppUrlResolver;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Task<StorefrontShellContext>? _cachedTask;

        public StorefrontShellContextService(
            IStorefrontDisplayContextProvider displayContextProvider,
            IStorefrontCatalogClient catalogClient,
            IStorefrontPageNavigationProvider pageNavigationProvider,
            IStorefrontNavigationProvider navigationProvider,
            IStorefrontSessionResolver sessionResolver,
            IStorefrontClientAppUrlResolver clientAppUrlResolver,
            IHttpContextAccessor httpContextAccessor)
        {
            _displayContextProvider = displayContextProvider;
            _catalogClient = catalogClient;
            _pageNavigationProvider = pageNavigationProvider;
            _navigationProvider = navigationProvider;
            _sessionResolver = sessionResolver;
            _clientAppUrlResolver = clientAppUrlResolver;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<StorefrontShellContext> GetAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                if (httpContext.Items.TryGetValue(CacheKey, out var cached) && cached is Task<StorefrontShellContext> cachedTask)
                {
                    return cachedTask;
                }

                var task = LoadAsync(cancellationToken);
                httpContext.Items[CacheKey] = task;
                return task;
            }

            return _cachedTask ??= LoadAsync(cancellationToken);
        }

        private async Task<StorefrontShellContext> LoadAsync(CancellationToken cancellationToken)
        {
            var displayTask = _displayContextProvider.GetAsync(cancellationToken);
            var sessionTask = _sessionResolver.GetCurrentUserAsync(cancellationToken);
            var categoryTask = _catalogClient.GetPublishedCategoryTreeAsync(cancellationToken);
            var headerLinksTask = _pageNavigationProvider.GetLinksByLocationAsync(StorefrontPageContentRules.Header, cancellationToken);
            var footerCompanyLinksTask = _pageNavigationProvider.GetLinksByLocationAsync(StorefrontPageContentRules.FooterCompany, cancellationToken);
            var footerSupportLinksTask = _pageNavigationProvider.GetLinksByLocationAsync(StorefrontPageContentRules.FooterSupport, cancellationToken);
            var footerLegalLinksTask = _pageNavigationProvider.GetLinksByLocationAsync(StorefrontPageContentRules.FooterLegal, cancellationToken);
            var mainMenuTask = _navigationProvider.GetMenuAsync(StoreNavigationMenuNames.Main, cancellationToken);
            var footerCompanyMenuTask = _navigationProvider.GetMenuAsync(StoreNavigationMenuNames.FooterCompany, cancellationToken);
            var footerSupportMenuTask = _navigationProvider.GetMenuAsync(StoreNavigationMenuNames.FooterSupport, cancellationToken);
            var footerLegalMenuTask = _navigationProvider.GetMenuAsync(StoreNavigationMenuNames.FooterLegal, cancellationToken);

            await Task.WhenAll(
                displayTask,
                sessionTask,
                categoryTask,
                headerLinksTask,
                footerCompanyLinksTask,
                footerSupportLinksTask,
                footerLegalLinksTask,
                mainMenuTask,
                footerCompanyMenuTask,
                footerSupportMenuTask,
                footerLegalMenuTask);

            var display = await displayTask;
            var returnUrl = ResolveSafeReturnUrl();
            var brand = CreateBrand(display);
            var navigation = new StorefrontNavigationContext(
                ToShellMenu(await mainMenuTask),
                ToShellMenu(await footerCompanyMenuTask),
                ToShellMenu(await footerSupportMenuTask),
                ToShellMenu(await footerLegalMenuTask),
                ToShellLinks(await headerLinksTask),
                ToShellLinks(await footerCompanyLinksTask),
                ToShellLinks(await footerSupportLinksTask),
                ToShellLinks(await footerLegalLinksTask));
            var search = new StorefrontSearchContext(
                StorefrontRoutes.Search,
                ToSearchCategoryLinks((await categoryTask).IsSuccess ? (await categoryTask).Value : null));
            var currency = new StorefrontCurrencyContext(
                display.CurrencyCode,
                display.DefaultCurrencyCode,
                display.SupportedCurrencyCodes,
                display.SupportedCurrencyCodes.Count > 1,
                StorefrontRoutes.CurrencyPreference,
                returnUrl);
            var account = CreateAccountMenu(await sessionTask);
            var header = new StorefrontHeaderContext(brand, navigation, search, currency, account, returnUrl);
            var footer = CreateFooter(brand, navigation, display);

            return new StorefrontShellContext(
                display,
                brand,
                header,
                footer,
                account,
                navigation,
                search,
                currency,
                returnUrl);
        }

        private string ResolveSafeReturnUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                return StorefrontRoutes.Home;
            }

            var returnUrl = $"{request.PathBase}{request.Path}{request.QueryString}";
            return IsSafeApplicationPath(returnUrl) ? returnUrl : StorefrontRoutes.Home;
        }

        private StorefrontAccountMenuContext CreateAccountMenu(StorefrontSessionInfo session)
        {
            if (!session.IsAuthenticated)
            {
                return new StorefrontAccountMenuContext(
                    false,
                    false,
                    null,
                    null,
                    [
                        new("Sign in", _clientAppUrlResolver.ResolveUrl(StorefrontRoutes.SignIn)),
                        new("Register", _clientAppUrlResolver.ResolveUrl(StorefrontRoutes.Register)),
                    ],
                    StorefrontRoutes.Home);
            }

            return new StorefrontAccountMenuContext(
                true,
                session.IsAdmin,
                session.DisplayName,
                session.Email,
                [
                    new("Profile", _clientAppUrlResolver.ResolveUrl(StorefrontRoutes.AccountProfile)),
                    new("Orders", _clientAppUrlResolver.ResolveUrl(StorefrontRoutes.AccountOrders)),
                    new("Addresses", _clientAppUrlResolver.ResolveUrl(StorefrontRoutes.AccountAddresses)),
                ],
                StorefrontRoutes.Home);
        }

        private static StorefrontFooterContext CreateFooter(
            StorefrontBrandContext brand,
            StorefrontNavigationContext navigation,
            StorefrontDisplayContext display)
        {
            var contactPhone = FirstNonEmptyOrNull(display.SupportPhone, display.CompanyPhone);
            return new StorefrontFooterContext(
                brand,
                navigation,
                $"© {DateTime.UtcNow.Year} {brand.Name}. All rights reserved.",
                FirstNonEmptyOrNull(display.SupportEmail, display.CompanyEmail),
                contactPhone,
                string.IsNullOrWhiteSpace(contactPhone) ? null : $"tel:{contactPhone.Replace(" ", string.Empty, StringComparison.Ordinal)}",
                display.CompanyAddress);
        }

        private static StorefrontBrandContext CreateBrand(StorefrontDisplayContext display)
        {
            var brandName = FirstNonEmptyOrNull(display.CompanyName, display.StoreName) ?? "BlazorShop";
            var brandLabel = string.IsNullOrWhiteSpace(display.CompanyName)
                ? display.StoreKey.ToUpperInvariant()
                : display.CompanyName;

            return new StorefrontBrandContext(
                brandName,
                brandLabel,
                $"{brandName} shop home",
                display.LogoUrl);
        }

        private static StorefrontShellMenu? ToShellMenu(StoreNavigationPublicMenuDto? menu)
        {
            if (menu is null)
            {
                return null;
            }

            return new StorefrontShellMenu(menu.SystemName, FlattenMenu(menu.Items).ToArray());
        }

        private static IReadOnlyList<StorefrontShellLink> FlattenMenu(IEnumerable<StoreNavigationPublicItemDto> items)
        {
            var links = new List<StorefrontShellLink>();
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Href))
                {
                    links.Add(new StorefrontShellLink(
                        item.Label.Trim(),
                        item.Href.Trim(),
                        item.OpensInNewTab ? "_blank" : null));
                }

                links.AddRange(FlattenMenu(item.Children));
            }

            return links;
        }

        private static IReadOnlyList<StorefrontShellLink> ToShellLinks(IReadOnlyList<StorefrontPageNavigationLinkDto> links)
        {
            return links
                .Where(link => !string.IsNullOrWhiteSpace(link.Slug) && !string.IsNullOrWhiteSpace(link.Title))
                .OrderBy(link => link.DisplayOrder)
                .ThenBy(link => link.Title, StringComparer.OrdinalIgnoreCase)
                .Select(link => new StorefrontShellLink(link.Title.Trim(), StorefrontRoutes.Page(link.Slug.Trim()), null, link.DisplayOrder))
                .ToArray();
        }

        private static IReadOnlyList<StorefrontShellLink> ToSearchCategoryLinks(IReadOnlyList<GetCategoryTreeNode>? categories)
        {
            if (categories is null || categories.Count == 0)
            {
                return [];
            }

            var links = new List<SearchCategoryLink>();
            foreach (var category in categories)
            {
                AppendCategory(links, category, 0);
            }

            return links
                .OrderBy(link => link.SortLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(link => link.Link.Label, StringComparer.OrdinalIgnoreCase)
                .Select(link => link.Link)
                .ToArray();
        }

        private static void AppendCategory(List<SearchCategoryLink> links, GetCategoryTreeNode category, int depth)
        {
            if (!string.IsNullOrWhiteSpace(category.Slug) && !string.IsNullOrWhiteSpace(category.Name))
            {
                var prefix = depth <= 0 ? string.Empty : $"{new string('-', depth * 2)} ";
                var name = category.Name.Trim();
                links.Add(new SearchCategoryLink(name, new StorefrontShellLink($"{prefix}{name}", category.Slug.Trim())));
            }

            foreach (var child in category.Children)
            {
                AppendCategory(links, child, depth + 1);
            }
        }

        private static string? FirstNonEmptyOrNull(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }

        private static bool IsSafeApplicationPath(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.StartsWith("/", StringComparison.Ordinal)
                && !value.StartsWith("//", StringComparison.Ordinal)
                && !value.Contains('\r')
                && !value.Contains('\n');
        }

        private sealed record SearchCategoryLink(string SortLabel, StorefrontShellLink Link);
    }
}
