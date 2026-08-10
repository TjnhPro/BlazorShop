namespace BlazorShop.Storefront.Presentation.Endpoints;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

using BlazorShop.Storefront.Client;
using BlazorShop.Storefront.Components.Contracts.Contact;
using BlazorShop.Storefront.Presentation.Contracts;
using BlazorShop.Storefront.Presentation.PagePatterns;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class StorefrontPresentationContactEndpoints
{
    public const string ContactRoute = "/api/contact";

    private static readonly EmailAddressAttribute EmailAddress = new();

    public static WebApplication MapStorefrontPresentationContactEndpoints(this WebApplication app)
    {
        app.MapPost(ContactRoute, async (
            StorefrontLocalContactRequest request,
            IStorefrontCurrentStoreProvider currentStoreProvider,
            IStorefrontContactClient contactClient,
            IAntiforgery antiforgery,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var antiforgeryFailure = await ValidateContactAntiforgeryAsync(httpContext, antiforgery);
            if (antiforgeryFailure is not null)
            {
                return antiforgeryFailure;
            }

            var fieldErrors = Validate(request);
            if (fieldErrors.Count > 0)
            {
                return ContactError(
                    StatusCodes.Status400BadRequest,
                    "validation_error",
                    "The contact request is invalid.",
                    fieldErrors);
            }

            var storeResolution = await currentStoreProvider.ResolveAsync(cancellationToken);
            if (storeResolution is not { Status: StorefrontCurrentStoreResolutionStatus.Success, Store.StoreKey.Length: > 0 })
            {
                return ContactError(
                    StoreResolutionStatusCode(storeResolution.Status),
                    StoreResolutionErrorCode(storeResolution.Status),
                    storeResolution.Message,
                    retryable: storeResolution.Status is StorefrontCurrentStoreResolutionStatus.ServiceUnavailable);
            }

            try
            {
                var response = await contactClient.SubmitAsync(
                    storeResolution.Store.StoreKey,
                    ToGeneratedRequest(request),
                    cancellationToken);

                if (response.Success == true && response.Data?.Accepted == true)
                {
                    return Results.Ok(ContactSuccess(response.Data.Message));
                }

                if (response.Success == true)
                {
                    return ContactError(
                        StatusCodes.Status400BadRequest,
                        "contact_rejected",
                        response.Data?.Message ?? response.Message);
                }

                return ContactError(
                    StatusCodes.Status503ServiceUnavailable,
                    "service_unavailable",
                    response.Message,
                    retryable: true);
            }
            catch (StorefrontApiException exception)
            {
                return ContactError(
                    NormalizeFailureStatusCode(exception.StatusCode),
                    "service_unavailable",
                    "The contact request could not be submitted.",
                    retryable: true);
            }
        });

        return app;
    }

    private static async Task<IResult?> ValidateContactAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return ContactError(
                StatusCodes.Status400BadRequest,
                "validation_error",
                "Security validation failed. Refresh the page and try again.",
                retryable: false);
        }
    }

    private static StorefrontContactRequest ToGeneratedRequest(StorefrontLocalContactRequest request)
    {
        return new StorefrontContactRequest
        {
            Name = request.Name!.Trim(),
            Email = request.Email!.Trim(),
            Subject = request.Subject!.Trim(),
            Message = request.Message!.Trim(),
        };
    }

    private static Dictionary<string, IReadOnlyList<string>> Validate(StorefrontLocalContactRequest request)
    {
        var errors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        AddRequired(errors, nameof(StorefrontLocalContactRequest.Name), request.Name);
        AddRequired(errors, nameof(StorefrontLocalContactRequest.Email), request.Email);
        AddRequired(errors, nameof(StorefrontLocalContactRequest.Subject), request.Subject);
        AddRequired(errors, nameof(StorefrontLocalContactRequest.Message), request.Message);

        if (!string.IsNullOrWhiteSpace(request.Email) && !EmailAddress.IsValid(request.Email))
        {
            errors[nameof(StorefrontLocalContactRequest.Email)] = ["Enter a valid email address."];
        }

        return errors;
    }

    private static void AddRequired(
        IDictionary<string, IReadOnlyList<string>> errors,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] = [$"{fieldName} is required."];
        }
    }

    private static IResult ContactError(
        int statusCode,
        string code,
        string? defaultMessage,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fieldErrors = null,
        bool retryable = false)
    {
        return Results.Json(
            new StorefrontContactFormSubmitResult(
                Success: false,
                Code: code,
                DefaultMessage: string.IsNullOrWhiteSpace(defaultMessage)
                    ? "The contact request could not be completed."
                    : defaultMessage,
                TraceId: Activity.Current?.TraceId.ToString(),
                FieldErrors: fieldErrors,
                Retryable: retryable),
            statusCode: statusCode);
    }

    private static StorefrontContactFormSubmitResult ContactSuccess(string? defaultMessage)
    {
        return new StorefrontContactFormSubmitResult(
            Success: true,
            DefaultMessage: string.IsNullOrWhiteSpace(defaultMessage)
                ? "Contact request accepted."
                : defaultMessage,
            TraceId: Activity.Current?.TraceId.ToString());
    }

    private static int StoreResolutionStatusCode(StorefrontCurrentStoreResolutionStatus status)
    {
        return status is StorefrontCurrentStoreResolutionStatus.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status503ServiceUnavailable;
    }

    private static string StoreResolutionErrorCode(StorefrontCurrentStoreResolutionStatus status)
    {
        return status is StorefrontCurrentStoreResolutionStatus.NotFound
            ? "store_not_found"
            : "store_unavailable";
    }

    private static int NormalizeFailureStatusCode(int statusCode)
    {
        return statusCode is >= 400 and < 600
            ? statusCode
            : StatusCodes.Status503ServiceUnavailable;
    }
}
