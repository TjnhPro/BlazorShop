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
            if (TryGetRawCookieValue(request, cookieName, out var refreshToken))
            {
                return BuildRefreshTokenCookieHeader(cookieName, refreshToken);
            }

            return request.Cookies.TryGetValue(cookieName, out refreshToken) && !string.IsNullOrWhiteSpace(refreshToken)
                ? BuildRefreshTokenCookieHeader(cookieName, refreshToken)
                : null;
        }

        public static string BuildRefreshTokenCookieHeader(string cookieName, string refreshToken)
        {
            return $"{cookieName}={refreshToken}";
        }

        private static bool TryGetRawCookieValue(HttpRequest request, string cookieName, out string refreshToken)
        {
            foreach (var cookieHeader in request.Headers.Cookie)
            {
                if (string.IsNullOrWhiteSpace(cookieHeader))
                {
                    continue;
                }

                var cookies = cookieHeader.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var cookie in cookies)
                {
                    var equalsIndex = cookie.IndexOf('=');
                    if (equalsIndex <= 0)
                    {
                        continue;
                    }

                    var name = cookie[..equalsIndex];
                    if (string.Equals(name, cookieName, StringComparison.Ordinal))
                    {
                        refreshToken = cookie[(equalsIndex + 1)..];
                        return !string.IsNullOrWhiteSpace(refreshToken);
                    }
                }
            }

            refreshToken = string.Empty;
            return false;
        }
    }
}
