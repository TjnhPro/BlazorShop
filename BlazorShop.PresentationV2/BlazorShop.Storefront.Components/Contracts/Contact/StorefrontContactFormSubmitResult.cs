namespace BlazorShop.Storefront.Components.Contracts.Contact;

public sealed record StorefrontContactFormSubmitResult(
    bool Success,
    string? Code = null,
    string? DefaultMessage = null,
    string? TraceId = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? FieldErrors = null,
    bool Retryable = false);
