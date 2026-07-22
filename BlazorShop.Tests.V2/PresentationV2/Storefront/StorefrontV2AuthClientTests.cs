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
                Assert.Equal("/api/storefront/stores/default/auth/login", request.RequestUri?.AbsolutePath);

                var response = JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"Signed in.","data":{"accessToken":"jwt-token","expiresAtUtc":"2026-07-14T00:00:00Z"}}""");
                response.Headers.TryAddWithoutValidation("Set-Cookie", "__Host-blazorshop-refresh=abc; Path=/; Secure; HttpOnly");
                return response;
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.LoginAsync(new LoginUser { Email = "customer@example.test", Password = "Password123!" });

            Assert.True(result.Success);
            Assert.Equal("Signed in.", result.Message);
            Assert.Equal("jwt-token", result.Data?.AccessToken);
            Assert.Contains("__Host-blazorshop-refresh=abc", result.SetCookieHeaders.Single(), StringComparison.Ordinal);
            Assert.Equal(["/api/storefront/stores/default/auth/login"], handler.RequestPaths);
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
                Assert.Equal("/api/storefront/stores/default/auth/register", request.RequestUri?.AbsolutePath);

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
            Assert.Equal(["/api/storefront/stores/default/auth/register"], handler.RequestPaths);
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

        [Fact]
        public async Task GetRegistrationPolicyAsync_ReadsScopedPolicyEndpoint()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/auth/registration-policy", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"Registration policy returned.","data":{"mode":"disabled","registrationAllowed":false,"message":"Customer registration is disabled."}}""");
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.GetRegistrationPolicyAsync();

            Assert.True(result.Success);
            Assert.Equal("disabled", result.Data?.Mode);
            Assert.False(result.Data?.RegistrationAllowed);
            Assert.Equal("Customer registration is disabled.", result.Data?.Message);
            Assert.Equal(["/api/storefront/stores/default/auth/registration-policy"], handler.RequestPaths);
        }

        [Fact]
        public async Task ForgotPasswordAsync_PostsScopedRecoveryRequest()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/auth/forgot-password", request.RequestUri?.AbsolutePath);
                var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using var document = JsonDocument.Parse(body);
                Assert.Equal("customer@example.test", document.RootElement.GetProperty("email").GetString());
                Assert.Equal("captcha-token", document.RootElement.GetProperty("captchaToken").GetString());

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"If the email exists, reset instructions will be sent.","data":null}""");
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.ForgotPasswordAsync("customer@example.test", "captcha-token");

            Assert.True(result.Success);
            Assert.Equal("If the email exists, reset instructions will be sent.", result.Message);
            Assert.Equal(["/api/storefront/stores/default/auth/forgot-password"], handler.RequestPaths);
        }

        [Fact]
        public async Task ResetPasswordAsync_PostsScopedResetRequest()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/auth/reset-password", request.RequestUri?.AbsolutePath);
                var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using var document = JsonDocument.Parse(body);
                Assert.Equal("customer@example.test", document.RootElement.GetProperty("email").GetString());
                Assert.Equal("reset-token", document.RootElement.GetProperty("token").GetString());
                Assert.Equal("NewPassword123!", document.RootElement.GetProperty("password").GetString());
                Assert.Equal("NewPassword123!", document.RootElement.GetProperty("confirmPassword").GetString());

                return JsonResponse(HttpStatusCode.OK, """{"success":true,"message":"Password reset.","data":null}""");
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.ResetPasswordAsync(
                "customer@example.test",
                "reset-token",
                "NewPassword123!",
                "NewPassword123!");

            Assert.True(result.Success);
            Assert.Equal("Password reset.", result.Message);
            Assert.Equal(["/api/storefront/stores/default/auth/reset-password"], handler.RequestPaths);
        }

        [Fact]
        public async Task LogoutAsync_SendsRefreshCookieAndCapturesExpiredCookie()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/auth/logout", request.RequestUri?.AbsolutePath);
                Assert.Equal("__Host-blazorshop-refresh=abc", request.Headers.GetValues("Cookie").Single());
                Assert.Equal("Storefront QA", request.Headers.UserAgent.ToString());

                var response = JsonResponse(HttpStatusCode.OK, """{"success":true,"message":"Signed out.","data":null}""");
                response.Headers.TryAddWithoutValidation("Set-Cookie", "__Host-blazorshop-refresh=; expires=Thu, 01 Jan 1970 00:00:00 GMT; Path=/; Secure; HttpOnly");
                return response;
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.LogoutAsync("__Host-blazorshop-refresh=abc", "Storefront QA");

            Assert.True(result.Success);
            Assert.Equal("Signed out.", result.Message);
            Assert.Contains("__Host-blazorshop-refresh=;", result.SetCookieHeaders.Single(), StringComparison.Ordinal);
            Assert.Equal(["/api/storefront/stores/default/auth/logout"], handler.RequestPaths);
        }

        [Fact]
        public async Task ChangePasswordAsync_PostsBearerProtectedCommand()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/auth/change-password", request.RequestUri?.AbsolutePath);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                return JsonResponse(HttpStatusCode.OK, """{"success":true,"message":"Password changed.","data":null}""");
            });

            var authClient = new StorefrontAuthClient(CreateClient(handler));

            var result = await authClient.ChangePasswordAsync(
                "access-token",
                new ChangePassword
                {
                    CurrentPassword = "OldPassword123!",
                    NewPassword = "NewPassword123!",
                    ConfirmPassword = "NewPassword123!",
                });

            Assert.True(result.Success);
            Assert.Equal("Password changed.", result.Message);
            Assert.Equal(["/api/storefront/stores/default/auth/change-password"], handler.RequestPaths);
        }

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
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
                var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
                Assert.DoesNotContain("/api/internal", requestPath, StringComparison.OrdinalIgnoreCase);
                this.requestPaths.Add(requestPath);
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
