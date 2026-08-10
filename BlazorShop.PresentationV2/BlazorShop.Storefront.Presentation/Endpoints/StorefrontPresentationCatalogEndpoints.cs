namespace BlazorShop.Storefront.Presentation.Endpoints;

using BlazorShop.Storefront.Presentation.Services.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class StorefrontPresentationCatalogEndpoints
{
    public static WebApplication MapStorefrontPresentationCatalogEndpoints(this WebApplication app)
    {
        app.MapGet(StorefrontDiscountedProductRailService.LocalRoute, async (
            [FromQuery] int? limit,
            StorefrontDiscountedProductRailService railService,
            CancellationToken cancellationToken) =>
        {
            if (!StorefrontDiscountedProductRailService.TryNormalizeLimit(limit, out var normalizedLimit, out var limitError))
            {
                return Results.Json(limitError, statusCode: StatusCodes.Status400BadRequest);
            }

            var response = await railService.GetAsync(normalizedLimit, cancellationToken);
            if (response.Success)
            {
                return Results.Ok(response);
            }

            var statusCode = response.Code is "service_unavailable"
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status502BadGateway;
            return Results.Json(response, statusCode: statusCode);
        });

        return app;
    }
}
