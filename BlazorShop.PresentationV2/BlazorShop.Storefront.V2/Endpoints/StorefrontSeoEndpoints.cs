namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Diagnostics;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using BlazorShop.Web.SharedV2.Models;

    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontSeoEndpoints
    {
        public static WebApplication MapStorefrontSeoEndpoints(this WebApplication app)
        {
            app.MapGet(StorefrontRoutes.Robots, async (HttpContext httpContext, IStorefrontRobotsService robotsService, CancellationToken cancellationToken) =>
            {
                try
                {
                    var content = await robotsService.GenerateAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        SeoRuntimeLogger.PublicDiscoveryRobotsFailure(app.Logger, StorefrontRoutes.Robots, "empty_document");
                        StorefrontResponseHeaders.ApplyServiceUnavailable(httpContext);
                        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                    }
            
                    StorefrontResponseHeaders.ApplyRobotsDocument(httpContext.Response);
                    return Results.Text(content, "text/plain; charset=utf-8");
                }
                catch (Exception exception)
                {
                    SeoRuntimeLogger.PublicDiscoveryRobotsFailure(app.Logger, exception, StorefrontRoutes.Robots, "generation_exception");
                    StorefrontResponseHeaders.ApplyServiceUnavailable(httpContext);
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            });
            app.MapGet(StorefrontRoutes.Sitemap, async (HttpContext httpContext, IStorefrontSitemapService sitemapService, CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await sitemapService.GenerateAsync(cancellationToken);
                    if (result.IsServiceUnavailable)
                    {
                        SeoRuntimeLogger.PublicDiscoverySitemapFailure(app.Logger, StorefrontRoutes.Sitemap, "upstream_service_unavailable");
                        StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
                        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                    }
            
                    if (string.IsNullOrWhiteSpace(result.Content))
                    {
                        SeoRuntimeLogger.PublicDiscoverySitemapFailure(app.Logger, StorefrontRoutes.Sitemap, "empty_document");
                        StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
                        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                    }
            
                    StorefrontResponseHeaders.ApplySitemapDocument(httpContext.Response);
                    return Results.Text(result.Content, "application/xml; charset=utf-8");
                }
                catch (Exception exception)
                {
                    SeoRuntimeLogger.PublicDiscoverySitemapFailure(app.Logger, exception, StorefrontRoutes.Sitemap, "generation_exception");
                    StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            });

            return app;
        }
    }
}

