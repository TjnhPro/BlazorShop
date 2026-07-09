namespace BlazorShop.Storefront.Services
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    public static class StorefrontAuthCookies
    {
        private const string DefaultRefreshTokenCookieName = "__Host-blazorshop-refresh";

        public static string GetRefreshTokenCookieName(IConfiguration configuration)
        {
            return string.IsNullOrWhiteSpace(configuration["Api:RefreshTokenCookieName"])
                ? DefaultRefreshTokenCookieName
                : configuration["Api:RefreshTokenCookieName"]!;
        }

        public static string? BuildRefreshTokenCookieHeader(HttpRequest request, IConfiguration configuration)
        {
            var cookieName = GetRefreshTokenCookieName(configuration);
            return request.Cookies.TryGetValue(cookieName, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken)
                ? $"{cookieName}={Uri.EscapeDataString(refreshToken)}"
                : null;
        }
    }
}
