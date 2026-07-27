namespace BlazorShop.Storefront.Presentation.Services.Auth;

public sealed record StorefrontAuthPageContext(
    StorefrontAuthPageKind Kind,
    string Title,
    string Eyebrow,
    string Description,
    string? Error,
    string? SuccessMessage,
    string SafeReturnUrl,
    bool ReturnUrlRejected,
    string PostAction,
    string SignInUrl,
    string RegisterUrl,
    string ForgotPasswordUrl,
    string? Email,
    string? Token,
    bool RegistrationAllowed,
    string RegistrationMessage,
    bool MissingRecoveryData);
