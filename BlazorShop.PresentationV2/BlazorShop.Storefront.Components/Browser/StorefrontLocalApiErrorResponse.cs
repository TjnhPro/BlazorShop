namespace BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontLocalApiErrorResponse(
    string? Message = null,
    string? Code = null,
    string? TraceId = null,
    Dictionary<string, string[]>? FieldErrors = null,
    bool? Retryable = null,
    int? StatusCode = null);
