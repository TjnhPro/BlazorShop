namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
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

    public static class StorefrontConsentEndpoints
    {
        public static WebApplication MapStorefrontConsentEndpoints(this WebApplication app)
        {
            app.MapGet("/api/consent/current", async (
                IStorefrontConsentClient apiClient,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
                var result = await apiClient.GetConsentAsync(visitorKey, cancellationToken);
                return result.Success
                    ? Results.Ok(result.Data)
                    : Results.Json(new StorefrontLocalCartErrorResponse(result.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
            });
            app.MapPost("/api/consent", async (
                StorefrontConsentSaveRequest request,
                IStorefrontConsentClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
                var result = await apiClient.SaveConsentAsync(visitorKey, request, cancellationToken);
                return result.Success
                    ? Results.Ok(result.Data)
                    : Results.Json(new StorefrontLocalCartErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
            });
            app.MapPost("/api/consent/revoke", async (
                IStorefrontConsentClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
                var result = await apiClient.RevokeConsentAsync(visitorKey, cancellationToken);
                return result.Success
                    ? Results.Ok(result.Data)
                    : Results.Json(new StorefrontLocalCartErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
            });

            return app;
        }
    }
}

