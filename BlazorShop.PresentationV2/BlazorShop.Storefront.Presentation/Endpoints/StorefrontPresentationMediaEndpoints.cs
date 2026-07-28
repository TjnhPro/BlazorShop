namespace BlazorShop.Storefront.Presentation.Endpoints
{
    using BlazorShop.Storefront.Presentation.Services.Media;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    public static class StorefrontPresentationMediaEndpoints
    {
        public static WebApplication MapStorefrontPresentationMediaEndpoints(this WebApplication app)
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

