namespace BlazorShop.Storefront.Components.Contracts.Contact;

public sealed record StorefrontContactFormSubmitRequest(
    string Name,
    string Email,
    string Subject,
    string Message);
