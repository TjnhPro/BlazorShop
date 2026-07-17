namespace BlazorShop.Storefront.Services
{
    using System.Net.Http.Json;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    public sealed class StorefrontSessionResolver : IStorefrontSessionResolver
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public StorefrontSessionResolver(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<StorefrontSessionInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return StorefrontSessionInfo.Anonymous;
            }

            var cookieName = GetRefreshTokenCookieName();
            if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                return StorefrontSessionInfo.Anonymous;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, GetRefreshTokenRoute());
            request.Headers.TryAddWithoutValidation("Cookie", $"{cookieName}={Uri.EscapeDataString(refreshToken)}");

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            CopySetCookieHeaders(response, httpContext.Response);

            if (!response.IsSuccessStatusCode)
            {
                return StorefrontSessionInfo.Anonymous;
            }

            var payload = await ReadTokenResponseAsync(response, cancellationToken);
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return StorefrontSessionInfo.Anonymous;
            }

            return ParseSession(payload.AccessToken);
        }

        private string GetRefreshTokenCookieName()
        {
            return StorefrontAuthCookies.GetRefreshTokenCookieName(_configuration);
        }

        private string GetRefreshTokenRoute()
        {
            return string.IsNullOrWhiteSpace(_configuration["Api:RefreshTokenRoute"])
                ? "auth/refresh-token"
                : _configuration["Api:RefreshTokenRoute"]!;
        }

        private static void CopySetCookieHeaders(HttpResponseMessage response, HttpResponse storefrontResponse)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return;
            }

            foreach (var value in values)
            {
                storefrontResponse.Headers.Append("Set-Cookie", value);
            }
        }

        private static StorefrontSessionInfo ParseSession(string token)
        {
            var tokenParts = token.Split('.');
            if (tokenParts.Length < 2)
            {
                return StorefrontSessionInfo.Anonymous;
            }

            try
            {
                using var document = JsonDocument.Parse(DecodeTokenPayload(tokenParts[1]));
                var root = document.RootElement;

                var roleClaims = ReadClaimValues(root, ClaimTypes.Role)
                    .Concat(ReadClaimValues(root, "role"));

                var isAdmin = roleClaims.Any(role => string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase));

                var displayName = ReadClaimValue(root, "FullName")
                    ?? ReadClaimValue(root, ClaimTypes.Name)
                    ?? ReadClaimValue(root, "unique_name")
                    ?? ReadClaimValue(root, ClaimTypes.Email)
                    ?? ReadClaimValue(root, "email");

                var email = ReadClaimValue(root, ClaimTypes.Email)
                    ?? ReadClaimValue(root, "email");

                return new StorefrontSessionInfo(true, isAdmin, displayName, email, token);
            }
            catch
            {
                return StorefrontSessionInfo.Anonymous;
            }
        }

        private static IEnumerable<string> ReadClaimValues(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                return [];
            }

            return property.ValueKind switch
            {
                JsonValueKind.Array => property.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()!)
                    .ToArray(),
                JsonValueKind.String => [property.GetString()!],
                _ => [],
            };
        }

        private static string? ReadClaimValue(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return property.GetString();
        }

        private static string DecodeTokenPayload(string encodedPayload)
        {
            var normalized = encodedPayload.Replace('-', '+').Replace('_', '/');
            var padded = (normalized.Length % 4) switch
            {
                2 => normalized + "==",
                3 => normalized + "=",
                _ => normalized,
            };

            return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }

        private static async Task<StorefrontTokenResponse?> ReadTokenResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("success", out _)
                && document.RootElement.TryGetProperty("data", out var dataProperty))
            {
                return dataProperty.ValueKind == JsonValueKind.Null
                    ? null
                    : dataProperty.Deserialize<StorefrontTokenResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }

            return document.RootElement.Deserialize<StorefrontTokenResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
    }
}
