namespace BlazorShop.Storefront.Configuration;

public sealed record StorefrontLocalCartErrorResponse(
    string Message,
    string? Code = null,
    string? TraceId = null,
    Dictionary<string, string[]>? FieldErrors = null,
    bool? Retryable = null,
    int? StatusCode = null);
