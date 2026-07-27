namespace BlazorShop.Storefront.Presentation.Endpoints
{
    using System.Diagnostics;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    public static class StorefrontPresentationConsentEndpoints
    {
        private const string StorefrontConsentVisitorCookieName = "bs-consent-visitor";

        public static WebApplication MapStorefrontPresentationConsentEndpoints(this WebApplication app)
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
                    : LocalConsentError(result.Message, StatusCodes.Status503ServiceUnavailable, "service_unavailable", retryable: true);
            });
            app.MapPost("/api/consent", async (
                StorefrontConsentSaveRequest request,
                IStorefrontConsentClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalConsentAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
                var result = await apiClient.SaveConsentAsync(visitorKey, request, cancellationToken);
                return result.Success
                    ? Results.Ok(result.Data)
                    : LocalConsentValidationError(result.Message);
            });
            app.MapPost("/api/consent/revoke", async (
                IStorefrontConsentClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var antiforgeryFailure = await ValidateLocalConsentAntiforgeryAsync(httpContext, antiforgery);
                if (antiforgeryFailure is not null)
                {
                    return antiforgeryFailure;
                }
            
                var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
                var result = await apiClient.RevokeConsentAsync(visitorKey, cancellationToken);
                return result.Success
                    ? Results.Ok(result.Data)
                    : LocalConsentValidationError(result.Message);
            });

            return app;
        }

        private static async Task<IResult?> ValidateLocalConsentAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
        {
            StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
                return null;
            }
            catch (AntiforgeryValidationException)
            {
                return LocalConsentValidationError("Security validation failed. Refresh the page and try again.");
            }
        }

        private static IResult LocalConsentValidationError(string? message)
        {
            return LocalConsentError(message, StatusCodes.Status400BadRequest, "validation_error");
        }

        private static IResult LocalConsentError(string? message, int statusCode, string code, bool? retryable = null)
        {
            return Results.Json(
                new StorefrontLocalCartErrorResponse(
                    string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message,
                    code,
                    Activity.Current?.TraceId.ToString(),
                    [],
                    retryable ?? statusCode is StatusCodes.Status408RequestTimeout or StatusCodes.Status429TooManyRequests || statusCode >= 500,
                    statusCode),
                statusCode: statusCode);
        }

        private static string ResolveConsentVisitorKey(HttpContext httpContext, bool createIfMissing)
        {
            if (httpContext.Request.Cookies.TryGetValue(StorefrontConsentVisitorCookieName, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            if (!createIfMissing)
            {
                return string.Empty;
            }

            var visitorKey = Guid.NewGuid().ToString("N");
            httpContext.Response.Cookies.Append(
                StorefrontConsentVisitorCookieName,
                visitorKey,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(180),
                });
            return visitorKey;
        }
    }
}

