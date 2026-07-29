namespace BlazorShop.Storefront.Presentation.Services.Account
{
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Presentation.Services;
    using BlazorShop.Storefront.Presentation.Services.Browser;
    using BlazorShop.Storefront.Presentation.Contracts;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Http;

    public sealed class StorefrontAccountPageService
    {
        private readonly IAntiforgery antiforgery;
        private readonly IStorefrontSessionResolver sessionResolver;
        private readonly StorefrontBrowserActionDescriptorProvider actionDescriptorProvider;

        public StorefrontAccountPageService(
            IAntiforgery antiforgery,
            IStorefrontSessionResolver sessionResolver,
            StorefrontBrowserActionDescriptorProvider actionDescriptorProvider)
        {
            this.antiforgery = antiforgery;
            this.sessionResolver = sessionResolver;
            this.actionDescriptorProvider = actionDescriptorProvider;
        }

        public async Task<StorefrontAccountPageResult> GetAsync(
            HttpContext? httpContext,
            string? path,
            int page,
            string? error,
            string? saved,
            CancellationToken cancellationToken = default)
        {
            StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

            var session = await this.sessionResolver.GetCurrentUserAsync(cancellationToken);
            if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                return StorefrontAccountPageResult.Redirect(StorefrontReturnUrl.BuildSignInUrl(CurrentReturnUrl(httpContext)));
            }

            var tokens = httpContext is null ? null : this.antiforgery.GetAndStoreTokens(httpContext);
            return StorefrontAccountPageResult.Ready(new StorefrontAccountPageContext(
                path,
                Math.Max(1, page),
                error,
                saved,
                tokens?.FormFieldName,
                tokens?.RequestToken)
            {
                ProfileActions = this.actionDescriptorProvider.CreateAccountProfileActions(),
                PasswordActions = this.actionDescriptorProvider.CreateAccountPasswordActions(),
                AddressActions = this.actionDescriptorProvider.CreateAccountAddressActions(),
                OrderActions = this.actionDescriptorProvider.CreateAccountOrderActions(),
                RouteDescriptor = this.actionDescriptorProvider.CreateAccountRouteDescriptor(),
                NavigationItems = this.actionDescriptorProvider.CreateAccountNavigationItems(),
            });
        }

        private static string CurrentReturnUrl(HttpContext? httpContext)
        {
            if (httpContext is null)
            {
                return StorefrontRoutes.Account;
            }

            var path = httpContext.Request.Path.HasValue
                ? httpContext.Request.Path.Value
                : StorefrontRoutes.Account;
            var query = httpContext.Request.QueryString.HasValue
                ? httpContext.Request.QueryString.Value
                : string.Empty;

            return StorefrontReturnUrl.Normalize(path + query, StorefrontRoutes.Account);
        }
    }

    public abstract record StorefrontAccountPageResult
    {
        public sealed record ReadyState(StorefrontAccountPageContext Context) : StorefrontAccountPageResult;

        public sealed record RedirectState(string Url) : StorefrontAccountPageResult;

        public static ReadyState Ready(StorefrontAccountPageContext context) => new(context);

        public static RedirectState Redirect(string url) => new(url);
    }
}
