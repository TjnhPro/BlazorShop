namespace BlazorShop.Storefront.Services
{
    using System.Collections.Concurrent;
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
        private static readonly object CurrentUserCacheKey = new();
        private static readonly ConcurrentDictionary<string, Lazy<Task<CachedRefreshSession>>> RefreshSessionCache = new(StringComparer.Ordinal);
        private static readonly TimeSpan RefreshReuseWindow = TimeSpan.FromSeconds(5);

        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public StorefrontSessionResolver(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public Task<StorefrontSessionInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return Task.FromResult(StorefrontSessionInfo.Anonymous);
            }

            if (httpContext.Items.TryGetValue(CurrentUserCacheKey, out var cached)
                && cached is Task<StorefrontSessionInfo> cachedTask)
            {
                return cachedTask;
            }

            lock (httpContext.Items)
            {
                if (httpContext.Items.TryGetValue(CurrentUserCacheKey, out cached)
                    && cached is Task<StorefrontSessionInfo> lockedCachedTask)
                {
                    return lockedCachedTask;
                }

                var task = ResolveCurrentUserAsync(httpContext, cancellationToken);
                httpContext.Items[CurrentUserCacheKey] = task;
                return task;
            }
        }

        private async Task<StorefrontSessionInfo> ResolveCurrentUserAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var cookieHeader = StorefrontAuthCookies.BuildRefreshTokenCookieHeader(httpContext.Request, _configuration);
            if (string.IsNullOrWhiteSpace(cookieHeader))
            {
                return StorefrontSessionInfo.Anonymous;
            }

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var cached = await ResolveRefreshSessionAsync(cookieHeader, userAgent, cancellationToken);
            CopySetCookieHeaders(cached.SetCookieHeaders, httpContext.Response);
            return cached.Session;
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

        private Task<CachedRefreshSession> ResolveRefreshSessionAsync(
            string cookieHeader,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"{cookieHeader}|{userAgent}";
            var lazy = RefreshSessionCache.GetOrAdd(
                cacheKey,
                key =>
                {
                    _ = Task.Delay(RefreshReuseWindow).ContinueWith(
                        _ =>
                        {
                            RefreshSessionCache.TryRemove(key, out var removed);
                        },
                        TaskScheduler.Default);
                    return new Lazy<Task<CachedRefreshSession>>(
                        () => this.ResolveRefreshSessionFromApiAsync(cookieHeader, userAgent, cancellationToken),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                });

            return lazy.Value;
        }

        private async Task<CachedRefreshSession> ResolveRefreshSessionFromApiAsync(
            string cookieHeader,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, GetRefreshTokenRoute());
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var setCookieHeaders = ReadSetCookieHeaders(response);

            if (!response.IsSuccessStatusCode)
            {
                return new CachedRefreshSession(StorefrontSessionInfo.Anonymous, setCookieHeaders);
            }

            var payload = await ReadTokenResponseAsync(response, cancellationToken);
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return new CachedRefreshSession(StorefrontSessionInfo.Anonymous, setCookieHeaders);
            }

            return new CachedRefreshSession(ParseSession(payload.AccessToken), setCookieHeaders);
        }

        private static void CopySetCookieHeaders(IReadOnlyList<string> values, HttpResponse storefrontResponse)
        {
            foreach (var value in values)
            {
                storefrontResponse.Headers.Append("Set-Cookie", value);
            }
        }

        private static IReadOnlyList<string> ReadSetCookieHeaders(HttpResponseMessage response)
        {
            return response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.ToArray()
                : [];
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

        private sealed record CachedRefreshSession(StorefrontSessionInfo Session, IReadOnlyList<string> SetCookieHeaders);
    }
}
