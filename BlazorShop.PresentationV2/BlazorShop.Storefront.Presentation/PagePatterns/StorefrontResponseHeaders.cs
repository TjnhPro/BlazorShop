namespace BlazorShop.Storefront.Presentation.PagePatterns;

using Microsoft.AspNetCore.Http;

public static class StorefrontResponseHeaders
{
    public static void ApplyPrivatePage(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return;
        }

        httpContext.Response.Headers["Cache-Control"] = StorefrontHttpStatusPolicy.PrivateCacheControl;
        httpContext.Response.Headers["X-Robots-Tag"] = StorefrontHttpStatusPolicy.NoIndexNoFollow;
        httpContext.Response.Headers.Remove("Retry-After");
    }

    public static void ApplyStatus(HttpContext? httpContext, StorefrontPageState state)
    {
        if (httpContext is null)
        {
            return;
        }

        httpContext.Response.StatusCode = StorefrontHttpStatusPolicy.ResolveStatusCode(state);
        if (StorefrontHttpStatusPolicy.IsPrivate(state))
        {
            ApplyPrivatePage(httpContext);
        }
    }
}
