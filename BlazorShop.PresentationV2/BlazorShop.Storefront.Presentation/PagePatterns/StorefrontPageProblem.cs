namespace BlazorShop.Storefront.Presentation.PagePatterns;

public sealed record StorefrontPageProblem(
    string Code,
    string Message,
    string? TraceId = null,
    bool Retryable = false,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null);
