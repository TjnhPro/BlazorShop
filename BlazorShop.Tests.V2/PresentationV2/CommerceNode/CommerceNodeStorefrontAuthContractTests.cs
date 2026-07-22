extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

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
        public async Task CurrentUserOrderDetail_WithoutBearerToken_ReturnsTypedUnauthorizedError()
        {
            using var client = this.CreateClient();

            using var response = await client.GetAsync("/api/storefront/stores/test-store/orders/current-user/ORD-1");

            await AssertTypedErrorAsync(response, HttpStatusCode.Unauthorized, "auth.unauthenticated");
        }

        [Fact]
        public async Task CurrentUserOrderReceipt_WithoutBearerToken_ReturnsTypedUnauthorizedError()
        {
            using var client = this.CreateClient();

            using var response = await client.GetAsync("/api/storefront/stores/test-store/orders/current-user/ORD-1/receipt");

            await AssertTypedErrorAsync(response, HttpStatusCode.Unauthorized, "auth.unauthenticated");
        }

        [Fact]
        public async Task RefreshToken_WithoutRefreshCookie_ReturnsTypedUnauthorizedError()
        {
            using var client = this.CreateClient();

            using var response = await client.PostAsync("/api/storefront/stores/test-store/auth/refresh-token", null);

            await AssertTypedErrorAsync(response, HttpStatusCode.Unauthorized, "auth.refresh_cookie_missing");
        }

        [Fact]
        public async Task Register_WhenRegistrationIsDisabled_ReturnsTypedForbiddenError()
        {
            using var client = this.CreateClient(settings =>
            {
                settings["Runtime:Security:RegistrationMode"] = "disabled";
            });

            using var response = await client.PostAsJsonAsync(
                "/api/storefront/stores/test-store/auth/register",
                new
                {
                    fullName = "Disabled Customer",
                    email = "disabled@example.test",
                    password = "Password123!",
                    confirmPassword = "Password123!",
                });

            await AssertTypedErrorAsync(response, HttpStatusCode.Forbidden, "auth.registration_disabled");
        }

        [Fact]
        public async Task RegistrationPolicy_ReturnsConfiguredModeWithoutAuthentication()
        {
            using var client = this.CreateClient(settings =>
            {
                settings["Runtime:Security:RegistrationMode"] = "disabled";
            });

            using var response = await client.GetAsync("/api/storefront/stores/test-store/auth/registration-policy");
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("disabled", body.RootElement.GetProperty("data").GetProperty("mode").GetString());
            Assert.False(body.RootElement.GetProperty("data").GetProperty("registrationAllowed").GetBoolean());
        }

        private HttpClient CreateClient(Action<IDictionary<string, string?>>? configureSettings = null)
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
                var settings = new Dictionary<string, string?>(StringComparer.Ordinal);
                configureSettings?.Invoke(settings);
                foreach (var setting in settings)
                {
                    builder.UseSetting(setting.Key, setting.Value);
                }

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ICommerceStoreDomainResolver>();
                    services.RemoveAll<IStoreSecurityPrivacySettingsService>();
                    services.AddScoped<ICommerceStoreDomainResolver, StubCommerceStoreDomainResolver>();
                    services.AddScoped<IStoreSecurityPrivacySettingsService>(_ =>
                        new StubStoreSecurityPrivacySettingsService(
                            settings.TryGetValue("Runtime:Security:RegistrationMode", out var registrationMode)
                                ? registrationMode
                                : null));
                });
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private static CommerceCurrentStore CreateCurrentStore()
        {
            return new CommerceCurrentStore(
                Guid.NewGuid(),
                "test-store",
                "Test Store",
                CommerceStoreStatuses.Active,
                "https://test-store.example",
                "test-store.example",
                true,
                null,
                null,
                "Test Store",
                "support@test-store.example",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "USD",
                "en-US",
                "support@test-store.example",
                null,
                false,
                null,
                null);
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

        private sealed class StubCommerceStoreDomainResolver : ICommerceStoreDomainResolver
        {
            private static readonly Guid StoreId = Guid.NewGuid();

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<StoreExecutionContext>> ResolveExecutionContextAsync(
                string? storeKey = null,
                string? host = null,
                string source = StoreExecutionContextSources.Unknown,
                CancellationToken cancellationToken = default)
            {
                var currentStore = CreateCurrentStore();
                return Task.FromResult(ApplicationResult<StoreExecutionContext>.Succeeded(
                    new StoreExecutionContext(
                        StoreId,
                        currentStore.StoreKey,
                        host,
                        source,
                        currentStore.Status,
                        true,
                        currentStore)));
            }

            public Task<ApplicationResult<Guid>> ResolveStoreIdAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<Guid>.Succeeded(StoreId));
            }
        }

        private sealed class StubStoreSecurityPrivacySettingsService : IStoreSecurityPrivacySettingsService
        {
            private readonly string registrationMode;

            public StubStoreSecurityPrivacySettingsService(string? registrationMode)
            {
                this.registrationMode = string.Equals(registrationMode, "disabled", StringComparison.OrdinalIgnoreCase)
                    ? "disabled"
                    : "standard";
            }

            public Task<ServiceResponse<StoreSecurityPrivacySettingsDto>> GetAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("The auth contract tests only resolve runtime settings.");
            }

            public Task<ServiceResponse<StoreSecurityPrivacySettingsDto>> UpdateAsync(
                UpdateStoreSecurityPrivacySettingsRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("The auth contract tests only resolve runtime settings.");
            }

            public Task<StoreSecurityPrivacyRuntimeSettings> ResolveCurrentAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new StoreSecurityPrivacyRuntimeSettings(
                    new StorefrontConsentOptions(),
                    new CaptchaOptions(),
                    new StoreRegistrationRuntimeSettings(
                        this.registrationMode,
                        !string.Equals(this.registrationMode, "disabled", StringComparison.Ordinal)),
                    new SecurityPrivacyOptions
                    {
                        DefaultRegistrationMode = this.registrationMode,
                    }));
            }
        }
    }
}
