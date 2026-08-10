namespace BlazorShop.Storefront.Components.Contracts.Contact;

public sealed record StorefrontContactFormClasses(
    string? Root = null,
    string? Form = null,
    string? Field = null,
    string? Label = null,
    string? Input = null,
    string? Textarea = null,
    string? Submit = null,
    string? Status = null,
    string? ErrorSummary = null,
    string? FieldError = null,
    string? SuccessMessage = null);
