using BlazorShop.Storefront.Components.Contracts.Contact;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShop.Storefront.Browser.Contact;

public sealed class StorefrontBrowserContactController : IStorefrontBrowserContactController
{
    public const string DefaultSubmitPath = "/api/contact";

    private readonly IServiceProvider _services;

    public StorefrontBrowserContactController(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public async Task<StorefrontContactFormSubmitResult> SubmitAsync(
        StorefrontContactFormSubmitRequest request,
        StorefrontContactFormActionDescriptor? actionDescriptor = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return Failure(
                "service_unavailable",
                "Contact submission is unavailable.",
                traceId: null,
                fieldErrors: null,
                retryable: true);
        }

        var route = string.IsNullOrWhiteSpace(actionDescriptor?.SubmitPath)
            ? DefaultSubmitPath
            : actionDescriptor.SubmitPath;
        var result = await apiClient
            .PostJsonAsync<StorefrontContactFormSubmitRequest, StorefrontContactFormSubmitResult>(
                route,
                request,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Success && result.Data is not null)
        {
            return result.Data;
        }

        if (result.Error is not null)
        {
            return Failure(
                result.Error.Code,
                result.Error.DisplayMessage,
                result.Error.TraceId,
                ToFieldErrors(result.Error.FieldErrors),
                result.Error.Retryable);
        }

        return Failure(
            "service_unavailable",
            string.IsNullOrWhiteSpace(result.Message)
                ? "Contact request could not be submitted."
                : result.Message,
            traceId: null,
            fieldErrors: null,
            retryable: true);
    }

    private StorefrontLocalApiClient? ResolveApiClient()
    {
        return _services.GetService<StorefrontLocalApiClient>();
    }

    private static StorefrontContactFormSubmitResult Failure(
        string code,
        string defaultMessage,
        string? traceId,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fieldErrors,
        bool retryable)
    {
        return new StorefrontContactFormSubmitResult(
            Success: false,
            Code: string.IsNullOrWhiteSpace(code) ? "service_unavailable" : code,
            DefaultMessage: defaultMessage,
            TraceId: traceId,
            FieldErrors: fieldErrors,
            Retryable: retryable);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? ToFieldErrors(
        IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        return fieldErrors.Count == 0
            ? null
            : fieldErrors.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
