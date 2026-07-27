namespace BlazorShop.Storefront.Presentation.Services.Account
{
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Http;

    public sealed class StorefrontAccountPageService
    {
        private readonly IAntiforgery antiforgery;
        private readonly IStorefrontSessionResolver sessionResolver;

        public StorefrontAccountPageService(
            IAntiforgery antiforgery,
            IStorefrontSessionResolver sessionResolver)
        {
            this.antiforgery = antiforgery;
            this.sessionResolver = sessionResolver;
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
                tokens?.RequestToken));
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
