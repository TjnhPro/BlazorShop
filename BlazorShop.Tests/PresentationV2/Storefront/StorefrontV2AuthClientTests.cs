extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.DTOs.UserIdentity;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontV2AuthClientTests
    {
        [Fact]
        public async Task LoginAsync_ReadsEnvelopeAndCapturesSetCookieHeaders()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/internal/auth/login", request.RequestUri?.AbsolutePath);

                var response = JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"Signed in.","data":{"success":true,"message":"Signed in.","token":"jwt-token","refreshToken":""}}""");
                response.Headers.TryAddWithoutValidation("Set-Cookie", "__Host-blazorshop-refresh=abc; Path=/; Secure; HttpOnly");
                return response;
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.LoginAsync(new LoginUser { Email = "customer@example.test", Password = "Password123!" });

            Assert.True(result.Success);
            Assert.Equal("Signed in.", result.Message);
            Assert.Equal("jwt-token", result.Data?.Token);
            Assert.Contains("__Host-blazorshop-refresh=abc", result.SetCookieHeaders.Single(), StringComparison.Ordinal);
            Assert.Equal(["/api/internal/auth/login"], handler.RequestPaths);
        }

        [Fact]
        public async Task LoginAsync_ReturnsApiFailureMessage()
        {
            var handler = new RecordingHandler(_ => JsonResponse(
                HttpStatusCode.BadRequest,
                """{"success":false,"message":"Invalid credentials.","data":null}"""));

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.LoginAsync(new LoginUser { Email = "customer@example.test", Password = "wrong" });

            Assert.False(result.Success);
            Assert.Equal("Invalid credentials.", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsApiSuccessMessage()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/internal/auth/create", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"User created successfully.","data":{"id":"00000000-0000-0000-0000-000000000001"}}""");
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.RegisterAsync(new CreateUser
            {
                FullName = "QA Customer",
                Email = "customer@example.test",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
            });

            Assert.True(result.Success);
            Assert.Equal("User created successfully.", result.Message);
            Assert.Equal(["/api/internal/auth/create"], handler.RequestPaths);
        }

        [Fact]
        public async Task RegisterAsync_WhenCommerceNodeUnavailable_ReturnsSafeFailure()
        {
            var authClient = new StorefrontAuthClient(CreateClient(new ThrowingHandler()));

            var result = await authClient.RegisterAsync(new CreateUser
            {
                FullName = "QA Customer",
                Email = "customer@example.test",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
            });

            Assert.False(result.Success);
            Assert.Equal("Unable to create your account right now.", result.Message);
            Assert.Empty(result.SetCookieHeaders);
        }

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/"),
            };
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> handler;
            private readonly List<string> requestPaths = [];

            public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                this.handler = handler;
            }

            public IReadOnlyList<string> RequestPaths => this.requestPaths;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.requestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
                return Task.FromResult(this.handler(request));
            }
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("Commerce Node unavailable.");
            }
        }
    }
}
