namespace BlazorShop.Storefront.Components.Contracts.Contact;

public sealed record StorefrontContactFormState(
    string Name,
    string Email,
    string Subject,
    string Message,
    bool IsSubmitting,
    bool Submitted,
    string? ErrorCode,
    string? DefaultMessage,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors)
{
    public static StorefrontContactFormState Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        IsSubmitting: false,
        Submitted: false,
        ErrorCode: null,
        DefaultMessage: null,
        FieldErrors: new Dictionary<string, IReadOnlyList<string>>());
}
