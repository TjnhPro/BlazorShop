namespace BlazorShop.Storefront.Configuration
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Web.SharedV2;

    using Microsoft.AspNetCore.Http;

    public static class StorefrontRateLimitIdentity
    {
        public static string ResolveLocalCartActor(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            var cartToken = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            if (!string.IsNullOrWhiteSpace(cartToken))
            {
                return $"cart:{HashToken(cartToken)}";
            }

            return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
            return Convert.ToHexString(bytes, 0, 16).ToLowerInvariant();
        }
    }
}
