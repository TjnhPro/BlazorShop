namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Application.DTOs.UserIdentity;

    public interface IStorefrontAuthClient
    {
        Task<StorefrontAuthResult<StorefrontTokenResponse>> LoginAsync(LoginUser user, CancellationToken cancellationToken = default);

        Task<StorefrontAuthResult<object>> RegisterAsync(CreateUser user, CancellationToken cancellationToken = default);

        Task<StorefrontAuthResult<StorefrontRegistrationPolicy>> GetRegistrationPolicyAsync(CancellationToken cancellationToken = default);

        Task<StorefrontAuthResult<object>> ForgotPasswordAsync(string email, string? captchaToken, CancellationToken cancellationToken = default);

        Task<StorefrontAuthResult<object>> ResetPasswordAsync(string email, string token, string password, string confirmPassword, CancellationToken cancellationToken = default);

        Task<StorefrontAuthResult<object>> ChangePasswordAsync(string bearerToken, ChangePassword changePassword, CancellationToken cancellationToken = default);

        Task<StorefrontAuthResult<object>> LogoutAsync(string? cookieHeader, string? userAgent, CancellationToken cancellationToken = default);
    }

    public sealed record StorefrontTokenResponse(string AccessToken, DateTime ExpiresAtUtc);

    public sealed record StorefrontRegistrationPolicy(
        string Mode,
        bool RegistrationAllowed,
        string Message);

    public sealed record StorefrontAuthResult<TData>(
        bool Success,
        string Message,
        TData? Data,
        IReadOnlyList<string> SetCookieHeaders)
    {
        public static StorefrontAuthResult<TData> Succeeded(TData? data, string? message, IReadOnlyList<string> setCookieHeaders)
        {
            return new StorefrontAuthResult<TData>(true, NormalizeMessage(message, "Authentication request completed."), data, setCookieHeaders);
        }

        public static StorefrontAuthResult<TData> Failed(string? message, IReadOnlyList<string>? setCookieHeaders = null)
        {
            return new StorefrontAuthResult<TData>(
                false,
                NormalizeMessage(message, "The authentication request could not be completed."),
                default,
                setCookieHeaders ?? []);
        }

        private static string NormalizeMessage(string? message, string fallback)
        {
            return string.IsNullOrWhiteSpace(message) ? fallback : message;
        }
    }
}
