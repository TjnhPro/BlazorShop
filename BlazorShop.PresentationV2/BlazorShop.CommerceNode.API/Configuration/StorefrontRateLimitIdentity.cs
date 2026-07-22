namespace BlazorShop.CommerceNode.API.Configuration
{
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;

    using Microsoft.AspNetCore.Http;

    public static class StorefrontRateLimitIdentity
    {
        public const string CartTokenHeaderName = "X-Cart-Token";

        public static string ResolveActor(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (httpContext.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }

            if (IsCartOrCheckoutPath(httpContext.Request.Path)
                && httpContext.Request.Headers.TryGetValue(CartTokenHeaderName, out var cartTokenValues))
            {
                var cartToken = cartTokenValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(cartToken))
                {
                    return $"cart:{HashToken(cartToken)}";
                }
            }

            return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        private static bool IsCartOrCheckoutPath(PathString path)
        {
            var value = path.Value ?? string.Empty;
            return value.Contains("/cart", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("/checkout", StringComparison.OrdinalIgnoreCase);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
            return Convert.ToHexString(bytes, 0, 16).ToLowerInvariant();
        }
    }
}
