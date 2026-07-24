namespace BlazorShop.Storefront.BuilderDemo.Endpoints
{
    using System.ComponentModel.DataAnnotations;
    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;
    using Microsoft.AspNetCore.Antiforgery;

    public static class StarterBffEndpoints
    {
        private const string CartTokenCookieName = "bs-starter-cart";

        public static IEndpointRouteBuilder MapStarterBffEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost(
                "/api/cart/lines",
                async (
                    StarterCartLineCommand command,
                    HttpContext httpContext,
                    IAntiforgery antiforgery,
                    IStorefrontRuntimeContext runtimeContext,
                    IStorefrontCartClient cartClient,
                    CancellationToken cancellationToken) =>
                {
                    var antiforgeryFailure = await ValidateAntiforgeryAsync(httpContext, antiforgery);
                    if (antiforgeryFailure is not null)
                    {
                        return antiforgeryFailure;
                    }

                    if (command.ProductId == Guid.Empty || command.Quantity < 1)
                    {
                        return StarterBffError.Validation("cart.validation", "Product and quantity are required.");
                    }

                    try
                    {
                        var cartToken = ReadCartToken(httpContext);
                        if (string.IsNullOrWhiteSpace(cartToken))
                        {
                            var session = await cartClient.CreateSessionAsync(
                                runtimeContext.StoreKey,
                                new StorefrontCreateCartSessionRequest(),
                                cancellationToken);
                            cartToken = session.Data?.CartToken;
                            if (!string.IsNullOrWhiteSpace(cartToken))
                            {
                                WriteCartToken(httpContext, cartToken);
                            }
                        }

                        var response = await cartClient.AddLineAsync(
                            cartToken,
                            runtimeContext.StoreKey,
                            new StorefrontCartLineCreateRequest
                            {
                                ProductId = command.ProductId,
                                ProductVariantId = command.ProductVariantId,
                                Quantity = command.Quantity,
                                CurrencyCode = command.CurrencyCode,
                            },
                            cancellationToken);

                        return Results.Json(new StarterBffSuccess(response.Data?.SummaryCount ?? 0));
                    }
                    catch (StorefrontApiException exception)
                    {
                        return StarterBffError.FromRuntimeError(StorefrontRuntimeErrorMapper.FromApiException(exception));
                    }
                });

            return endpoints;
        }

        private static async Task<IResult?> ValidateAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
                return null;
            }
            catch (AntiforgeryValidationException)
            {
                return StarterBffError.FromRuntimeError(
                    new StorefrontRuntimeError(
                        StatusCodes.Status403Forbidden,
                        "security.csrf",
                        "Security validation failed.",
                        httpContext.TraceIdentifier,
                        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)));
            }
        }

        private static string? ReadCartToken(HttpContext httpContext)
        {
            return httpContext.Request.Cookies.TryGetValue(CartTokenCookieName, out var token)
                && !string.IsNullOrWhiteSpace(token)
                ? token
                : null;
        }

        private static void WriteCartToken(HttpContext httpContext, string cartToken)
        {
            httpContext.Response.Cookies.Append(
                CartTokenCookieName,
                cartToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(30),
                });
        }
    }

    public sealed record StarterCartLineCommand(
        [property: Required] Guid ProductId,
        Guid? ProductVariantId,
        [property: Range(1, int.MaxValue)] int Quantity,
        [property: StringLength(3, MinimumLength = 3)] string? CurrencyCode);

    public sealed record StarterBffSuccess(int CartLineCount);

    public sealed record StarterBffError(
        int Status,
        string Code,
        string Message,
        string? TraceId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors)
    {
        public static IResult Validation(string code, string message)
        {
            return FromRuntimeError(
                new StorefrontRuntimeError(
                    StatusCodes.Status422UnprocessableEntity,
                    code,
                    message,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)));
        }

        public static IResult FromRuntimeError(StorefrontRuntimeError error)
        {
            return Results.Json(
                new StarterBffError(error.Status, error.Code, error.Message, error.TraceId, error.FieldErrors),
                statusCode: error.Status);
        }
    }
}

