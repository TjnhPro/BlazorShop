namespace BlazorShop.Storefront.Presentation.PagePatterns;

using Microsoft.AspNetCore.Http;

public static class StorefrontResponseHeaders
{
    public const string ErrorCacheControl = "no-store, no-cache, max-age=0";
    public const string RobotsCacheControl = "public, max-age=3600, must-revalidate";
    public const string SitemapCacheControl = "public, max-age=900, must-revalidate";
    public const string RetryAfterSeconds = "600";

    public static void ApplyNotFound(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return;
        }

        ApplyError(httpContext.Response, StatusCodes.Status404NotFound, includeRetryAfter: false);
    }

    public static void ApplyServiceUnavailable(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return;
        }

        ApplyError(httpContext.Response, StatusCodes.Status503ServiceUnavailable, includeRetryAfter: true);
    }

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

        if (state is StorefrontPageState.NotFoundState)
        {
            ApplyNotFound(httpContext);
            return;
        }

        if (state is StorefrontPageState.ServiceUnavailableState || state is StorefrontPageState.MaintenanceState)
        {
            ApplyServiceUnavailable(httpContext);
            return;
        }

        httpContext.Response.StatusCode = StorefrontHttpStatusPolicy.ResolveStatusCode(state);
        if (StorefrontHttpStatusPolicy.IsPrivate(state))
        {
            ApplyPrivatePage(httpContext);
        }
    }

    public static void ApplyRobotsDocument(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Headers["Cache-Control"] = RobotsCacheControl;
        response.Headers.Remove("Retry-After");
    }

    public static void ApplySitemapDocument(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Headers["Cache-Control"] = SitemapCacheControl;
        response.Headers.Remove("Retry-After");
    }

    public static void ApplySitemapUnavailable(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        response.Headers["Cache-Control"] = ErrorCacheControl;
        response.Headers["Retry-After"] = RetryAfterSeconds;
    }

    public static void RegisterErrorStatusHeaders(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(static state =>
        {
            ApplyErrorHeadersForStatus((HttpResponse)state);
            return Task.CompletedTask;
        }, context.Response);
    }

    private static void ApplyError(HttpResponse response, int statusCode, bool includeRetryAfter)
    {
        if (response.HasStarted)
        {
            return;
        }

        response.StatusCode = statusCode;
        response.OnStarting(static state =>
        {
            var options = ((HttpResponse Response, int StatusCode, bool IncludeRetryAfter))state;
            options.Response.StatusCode = options.StatusCode;
            ApplyErrorHeadersForStatus(options.Response, options.IncludeRetryAfter);
            return Task.CompletedTask;
        }, (response, statusCode, includeRetryAfter));
    }

    private static void ApplyErrorHeadersForStatus(HttpResponse response)
    {
        if (response.StatusCode == StatusCodes.Status404NotFound)
        {
            ApplyErrorHeadersForStatus(response, includeRetryAfter: false);
        }
        else if (response.StatusCode == StatusCodes.Status503ServiceUnavailable)
        {
            ApplyErrorHeadersForStatus(response, includeRetryAfter: true);
        }
    }

    private static void ApplyErrorHeadersForStatus(HttpResponse response, bool includeRetryAfter)
    {
        response.Headers["Cache-Control"] = ErrorCacheControl;
        response.Headers["X-Robots-Tag"] = StorefrontHttpStatusPolicy.NoIndexNoFollow;

        if (includeRetryAfter)
        {
            response.Headers["Retry-After"] = RetryAfterSeconds;
        }
        else
        {
            response.Headers.Remove("Retry-After");
        }
    }
}
