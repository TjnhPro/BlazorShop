extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;

    using Xunit;

    using CommerceNodeProgram = CommerceNodeApi::Program;

    public sealed class CommerceNodeStorefrontAuthContractTests : IClassFixture<WebApplicationFactory<CommerceNodeProgram>>
    {
        private readonly WebApplicationFactory<CommerceNodeProgram> factory;

        public CommerceNodeStorefrontAuthContractTests(WebApplicationFactory<CommerceNodeProgram> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task ChangePassword_WithoutBearerToken_ReturnsTypedUnauthorizedError()
        {
            using var client = this.CreateClient();

            using var response = await client.PostAsJsonAsync(
                "/api/storefront/stores/test-store/auth/change-password",
                new
                {
                    currentPassword = "OldPassword1!",
                    newPassword = "NewPassword1!",
                    confirmPassword = "NewPassword1!",
                });

            await AssertTypedErrorAsync(response, HttpStatusCode.Unauthorized, "auth.unauthenticated");
        }

        [Fact]
        public async Task CurrentUserOrders_WithoutBearerToken_ReturnsTypedUnauthorizedError()
        {
            using var client = this.CreateClient();

            using var response = await client.GetAsync("/api/storefront/stores/test-store/orders/current-user");

            await AssertTypedErrorAsync(response, HttpStatusCode.Unauthorized, "auth.unauthenticated");
        }

        [Fact]
        public async Task RefreshToken_WithoutRefreshCookie_ReturnsTypedUnauthorizedError()
        {
            using var client = this.CreateClient();

            using var response = await client.PostAsync("/api/storefront/stores/test-store/auth/refresh-token", null);

            await AssertTypedErrorAsync(response, HttpStatusCode.Unauthorized, "auth.refresh_cookie_missing");
        }

        private HttpClient CreateClient()
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private static async Task AssertTypedErrorAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatus,
            string expectedCode)
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(expectedCode, body.RootElement.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("message").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
        }
    }
}
