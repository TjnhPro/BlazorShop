namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Storefront.Services.Media;
    using BlazorShop.Web.SharedV2;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontMediaEndpoints
    {
        public static WebApplication MapStorefrontMediaEndpoints(this WebApplication app)
        {
            app.MapGet("/media/products/{mediaPublicId:guid}", async (
                Guid mediaPublicId,
                HttpContext httpContext,
                StorefrontMediaProxyService mediaProxyService,
                CancellationToken cancellationToken) =>
            {
                return await mediaProxyService.ProxyAsync(
                    $"media/products/{mediaPublicId:D}",
                    httpContext,
                    cancellationToken);
            });
            app.MapGet("/media/assets/{assetPublicId:guid}/{fileName}", async (
                Guid assetPublicId,
                string fileName,
                HttpContext httpContext,
                StorefrontMediaProxyService mediaProxyService,
                CancellationToken cancellationToken) =>
            {
                return await mediaProxyService.ProxyAsync(
                    $"media/assets/{assetPublicId:D}/{Uri.EscapeDataString(fileName)}",
                    httpContext,
                    cancellationToken);
            });

            return app;
        }
    }
}

