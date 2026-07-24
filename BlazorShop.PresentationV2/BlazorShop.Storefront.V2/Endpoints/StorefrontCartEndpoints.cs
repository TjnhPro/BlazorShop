namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;

    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontCartEndpoints
    {
        public static WebApplication MapStorefrontCartEndpoints(this WebApplication app)
        {
            app.MapGet("/api/cart", async (
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await cartTokenService.ResolveAsync(httpContext, cancellationToken: cancellationToken);
                var displayContext = await displayContextProvider.GetAsync(cancellationToken);
                return result.Success
                    ? Results.Ok(ToLocalCartResponse(result.Cart, displayContext, priceFormatter))
                    : Results.Ok(ToLocalCartResponse(null, displayContext, priceFormatter));
            });
            app.MapPost("/api/product-selection-preview", async (
                StorefrontLocalProductSelectionPreviewRequest request,
                IStorefrontCatalogClient apiClient,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                CancellationToken cancellationToken) =>
            {
                if (request.ProductId == Guid.Empty || request.Quantity < 1)
                {
                    return LocalCartValidationError("Product and quantity are required.");
                }
            
                var displayContext = await displayContextProvider.GetAsync(cancellationToken);
                var currencyCode = NormalizeCurrencyCode(request.CurrencyCode) ?? displayContext.CurrencyCode;
                var result = await apiClient.PreviewProductSelectionAsync(
                    request.ProductId,
                    new StorefrontProductSelectionPreviewRequest
                    {
                        ProductVariantId = request.ProductVariantId,
                        SelectedAttributes = request.SelectedAttributes,
                        Quantity = request.Quantity,
                        CurrencyCode = currencyCode,
                    },
                    cancellationToken);
            
                if (!result.Success || result.Data is null)
                {
                    return LocalCartValidationError(result.Message);
                }
            
                var preview = result.Data;
                var previewContext = displayContext with { CurrencyCode = preview.CurrencyCode };
                return Results.Ok(new StorefrontLocalProductSelectionPreviewResponse(
                    preview.ProductId,
                    preview.ProductVariantId,
                    preview.IsValid,
                    preview.IsAvailable,
                    preview.CanAddToCart,
                    preview.ValidationMessages,
                    preview.SelectedAttributes
                        .Select(attribute => new StorefrontSelectedAttribute(attribute.Name, attribute.Value))
                        .ToArray(),
                    preview.AttributeSignature,
                    preview.Sku,
                    preview.DisplayName,
                    preview.UnitPrice,
                    preview.ComparePrice,
                    preview.CurrencyCode,
                    priceFormatter.Format(preview.UnitPrice, previewContext),
                    preview.ComparePrice.HasValue ? priceFormatter.Format(preview.ComparePrice.Value, previewContext) : null,
                    preview.StockQuantity,
                    preview.MinQuantity,
                    preview.MaxQuantity,
                    preview.PrimaryImageUrl));
            });
            app.MapPost("/api/cart/lines", async (
                StorefrontLocalCartLineRequest request,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                if (request.ProductId == Guid.Empty || request.Quantity < 1)
                {
                    return LocalCartValidationError("Product and quantity are required.");
                }
            
                var result = await cartTokenService.AddLineAsync(
                    httpContext,
                    new StorefrontCartLineCreateRequest
                    {
                        ProductId = request.ProductId,
                        ProductVariantId = request.ProductVariantId,
                        CurrencyCode = request.CurrencyCode,
                        Quantity = request.Quantity,
                        SelectedAttributes = request.SelectedAttributes,
                    },
                    cancellationToken);
            
                return await ToLocalCartMutationResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontRateLimitPolicies.LocalCartPolicyName);
            app.MapPut("/api/cart/lines/{lineId:guid}", async (
                Guid lineId,
                StorefrontLocalCartQuantityRequest request,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                if (request.Quantity < 1)
                {
                    return LocalCartValidationError("Quantity must be at least 1.");
                }
            
                var result = await cartTokenService.UpdateLineAsync(httpContext, lineId, request.Quantity, cancellationToken);
                return await ToLocalCartMutationResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontRateLimitPolicies.LocalCartPolicyName);
            app.MapDelete("/api/cart/lines/{lineId:guid}", async (
                Guid lineId,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                var result = await cartTokenService.RemoveLineAsync(httpContext, lineId, cancellationToken);
                return await ToLocalCartMutationResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontRateLimitPolicies.LocalCartPolicyName);
            app.MapDelete("/api/cart", async (
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                var result = await cartTokenService.ClearAsync(httpContext, cancellationToken);
                return await ToLocalCartMutationResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontRateLimitPolicies.LocalCartPolicyName);

            return app;
        }
    }
}

