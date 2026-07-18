extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontSessionResolverTests
    {
        [Fact]
        public async Task GetCurrentUserAsync_CachesRefreshWithinHttpRequest()
        {
            var handler = new RefreshTokenHandler(CreateAccessToken());
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
            };
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Cookie = "__Host-blazorshop-refresh=initial%252Brefresh%252Ftoken";
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var configuration = new ConfigurationBuilder().Build();
            var firstResolver = new StorefrontSessionResolver(httpClient, accessor, configuration);
            var secondResolver = new StorefrontSessionResolver(httpClient, accessor, configuration);

            var sessions = await Task.WhenAll(
                firstResolver.GetCurrentUserAsync(),
                secondResolver.GetCurrentUserAsync());

            Assert.All(sessions, session =>
            {
                Assert.True(session.IsAuthenticated);
                Assert.Equal("qa.customer@example.local", session.Email);
            });
            Assert.Equal(1, handler.RequestCount);
            Assert.Single(httpContext.Response.Headers.SetCookie);
        }

        private static string CreateAccessToken()
        {
            var payload = JsonSerializer.Serialize(new
            {
                email = "qa.customer@example.local",
                FullName = "QA Customer",
            });

            return $"{Base64UrlEncode("{}")}.{Base64UrlEncode(payload)}.signature";
        }

        private static string Base64UrlEncode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private sealed class RefreshTokenHandler : HttpMessageHandler
        {
            private readonly string _accessToken;

            public RefreshTokenHandler(string accessToken)
            {
                _accessToken = accessToken;
            }

            public int RequestCount { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/storefront/stores/default/auth/refresh-token", request.RequestUri?.AbsolutePath);
                var cookieHeader = request.Headers.GetValues("Cookie").Single();
                Assert.DoesNotContain("%25252B", cookieHeader);
                Assert.DoesNotContain("%25252F", cookieHeader);
                Assert.Contains("__Host-blazorshop-refresh=initial%252Brefresh%252Ftoken", cookieHeader);

                await Task.Delay(25, cancellationToken);

                var payload = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ok",
                    data = new
                    {
                        accessToken = _accessToken,
                        refreshToken = (string?)null,
                    },
                });
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        payload,
                        Encoding.UTF8,
                        "application/json"),
                };
                response.Headers.TryAddWithoutValidation(
                    "Set-Cookie",
                    "__Host-blazorshop-refresh=rotated-refresh-token; path=/; secure; httponly; samesite=strict");
                return response;
            }
        }
    }
}
