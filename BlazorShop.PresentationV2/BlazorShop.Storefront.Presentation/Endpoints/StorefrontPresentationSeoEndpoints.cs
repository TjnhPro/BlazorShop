namespace BlazorShop.Storefront.Presentation.Endpoints;

using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class StorefrontPresentationSeoEndpoints
{
    public static IEndpointRouteBuilder MapStorefrontPresentationSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(StorefrontRoutes.Robots, GetRobotsAsync);
        endpoints.MapGet(StorefrontRoutes.Sitemap, GetSitemapAsync);

        return endpoints;
    }

    private static async Task<IResult> GetRobotsAsync(
        HttpContext httpContext,
        IStorefrontRobotsService robotsService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(StorefrontPresentationSeoEndpoints));

        try
        {
            var content = await robotsService.GenerateAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
            {
                SeoRuntimeLogger.PublicDiscoveryRobotsFailure(logger, StorefrontRoutes.Robots, "empty_document");
                StorefrontResponseHeaders.ApplyServiceUnavailable(httpContext);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            StorefrontResponseHeaders.ApplyRobotsDocument(httpContext.Response);
            return Results.Text(content, "text/plain; charset=utf-8");
        }
        catch (Exception exception)
        {
            SeoRuntimeLogger.PublicDiscoveryRobotsFailure(logger, exception, StorefrontRoutes.Robots, "generation_exception");
            StorefrontResponseHeaders.ApplyServiceUnavailable(httpContext);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> GetSitemapAsync(
        HttpContext httpContext,
        IStorefrontSitemapService sitemapService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(StorefrontPresentationSeoEndpoints));

        try
        {
            var result = await sitemapService.GenerateAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsServiceUnavailable)
            {
                SeoRuntimeLogger.PublicDiscoverySitemapFailure(logger, StorefrontRoutes.Sitemap, "upstream_service_unavailable");
                StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (string.IsNullOrWhiteSpace(result.Content))
            {
                SeoRuntimeLogger.PublicDiscoverySitemapFailure(logger, StorefrontRoutes.Sitemap, "empty_document");
                StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            StorefrontResponseHeaders.ApplySitemapDocument(httpContext.Response);
            return Results.Text(result.Content, "application/xml; charset=utf-8");
        }
        catch (Exception exception)
        {
            SeoRuntimeLogger.PublicDiscoverySitemapFailure(logger, exception, StorefrontRoutes.Sitemap, "generation_exception");
            StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
