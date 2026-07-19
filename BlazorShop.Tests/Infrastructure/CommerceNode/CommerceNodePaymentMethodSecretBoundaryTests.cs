namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class CommerceNodePaymentMethodSecretBoundaryTests
    {
        private const string SecretSettingsJson = "{\"apiKey\":\"super-secret\",\"publishableKey\":\"pk_test\"}";

        [Fact]
        public async Task GetAsync_ReturnsSettingsStatusWithoutRawSettingsJson()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedConfiguredPaymentMethodAsync(context, storeId);
            var service = CreateService(context, storeId);

            var methods = await service.GetAsync();

            var method = Assert.Single(methods, item => item.PaymentMethodKey == PaymentMethodKeys.Stripe);
            Assert.True(method.Settings.Configured);
            Assert.True(method.Capability.Installed);
            Assert.True(method.Capability.Active);
            Assert.Equal(PaymentProviderMethodTypes.Redirect, method.Capability.MethodType);
            Assert.True(method.Capability.RequiresWebhookSignature);
            var serialized = JsonSerializer.Serialize(method);
            Assert.DoesNotContain("super-secret", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("settingsJson", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetPaymentMethodsAsync_ReturnsSafePublicMetadata()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedConfiguredPaymentMethodAsync(
                context,
                storeId,
                method =>
                {
                    method.ShortDisplayText = "Cards";
                    method.IconUrl = "/media/assets/stripe.svg";
                    method.SupportedCurrencyCodesJson = "[\"USD\",\"EUR\"]";
                    method.SupportedCountryCodesJson = "[\"US\",\"CA\"]";
                });
            var service = CreateService(context, storeId);

            var methods = (await service.GetPaymentMethodsAsync()).ToArray();

            var stripe = Assert.Single(methods, method => method.Key == PaymentMethodKeys.Stripe);
            Assert.Equal("Cards", stripe.ShortDisplayText);
            Assert.Equal("/media/assets/stripe.svg", stripe.IconUrl);
            Assert.Equal(["USD", "EUR"], stripe.SupportedCurrencyCodes);
            Assert.Equal(["US", "CA"], stripe.SupportedCountryCodes);
        }

        [Fact]
        public async Task GetPaymentMethodsAsync_OrdersEnabledMethodsByDisplayOrder()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            context.StorePaymentMethods.AddRange(
                new StorePaymentMethod
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    PaymentMethodKey = PaymentMethodKeys.Cod,
                    Enabled = true,
                    DisplayName = "Cash",
                    DisplayOrder = 20,
                },
                new StorePaymentMethod
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    PaymentMethodKey = PaymentMethodKeys.Stripe,
                    Enabled = true,
                    DisplayName = "Cards",
                    DisplayOrder = 10,
                });
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var methods = (await service.GetPaymentMethodsAsync()).ToArray();

            Assert.Equal([PaymentMethodKeys.Stripe, PaymentMethodKeys.Cod], methods.Select(method => method.Key).ToArray());
        }

        [Fact]
        public async Task UpdateAsync_WhenSettingsJsonIsNull_PreservesExistingSettings()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedConfiguredPaymentMethodAsync(context, storeId);
            var auditService = new CapturingAdminAuditService();
            var service = CreateService(context, storeId, auditService);

            var result = await service.UpdateAsync(
                PaymentMethodKeys.Stripe,
                new UpdateStorePaymentMethodRequest(
                    Enabled: true,
                    DisplayName: "Stripe Cards",
                    Description: "Updated display copy.",
                    DisplayOrder: 5,
                    SettingsJson: null));

            Assert.True(result.Success, result.Message);
            Assert.True(result.Payload!.Settings.Configured);
            var method = await context.StorePaymentMethods.SingleAsync(item => item.StoreId == storeId && item.PaymentMethodKey == PaymentMethodKeys.Stripe);
            Assert.Equal(SecretSettingsJson, method.SettingsJson);
            Assert.DoesNotContain("super-secret", auditService.MetadataJson, StringComparison.Ordinal);
            Assert.DoesNotContain("SettingsJson", auditService.MetadataJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_WhenClearSettingsIsTrue_RemovesSettingsWithoutEchoingSecret()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedConfiguredPaymentMethodAsync(context, storeId);
            var service = CreateService(context, storeId);

            var result = await service.UpdateAsync(
                PaymentMethodKeys.Stripe,
                new UpdateStorePaymentMethodRequest(
                    Enabled: true,
                    DisplayName: "Stripe",
                    Description: "Card payments.",
                    DisplayOrder: 5,
                    SettingsJson: null,
                    ClearSettings: true));

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.Settings.Configured);
            var method = await context.StorePaymentMethods.SingleAsync(item => item.StoreId == storeId && item.PaymentMethodKey == PaymentMethodKeys.Stripe);
            Assert.Null(method.SettingsJson);
            var serialized = JsonSerializer.Serialize(result.Payload);
            Assert.DoesNotContain("super-secret", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("settingsJson", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_WhenProviderIsInactive_RejectsEnable()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.UpdateAsync(
                PaymentMethodKeys.PayPal,
                new UpdateStorePaymentMethodRequest(
                    Enabled: true,
                    DisplayName: "PayPal",
                    Description: "PayPal checkout.",
                    DisplayOrder: 30,
                    SettingsJson: null));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Payment provider is not installed or active.", result.Message);
        }

        private static async Task SeedConfiguredPaymentMethodAsync(
            CommerceNodeDbContext context,
            Guid storeId,
            Action<StorePaymentMethod>? configure = null)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            var method = new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = PaymentMethodKeys.Stripe,
                Enabled = true,
                DisplayName = "Stripe",
                Description = "Card payments.",
                DisplayOrder = 5,
                SettingsJson = SecretSettingsJson,
            };
            configure?.Invoke(method);

            context.StorePaymentMethods.Add(method);
            await context.SaveChangesAsync();
        }

        private static CommerceNodePaymentMethodService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            IAdminAuditService? auditService = null)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            return new CommerceNodePaymentMethodService(
                context,
                new StubCommerceStoreContext(storeId),
                auditService ?? new CapturingAdminAuditService(),
                new StorefrontPublicConfigurationCache(context, cache),
                new PaymentProviderCapabilityRegistry([new FakePaymentProvider(PaymentMethodKeys.Stripe)]));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"payment-method-secrets-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StubCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public StubCommerceStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", this.storeId));
            }
        }

        private sealed class CapturingAdminAuditService : IAdminAuditService
        {
            public string MetadataJson { get; private set; } = string.Empty;

            public Task<PagedResult<AdminAuditLogDto>> GetAsync(AdminAuditQueryDto query)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> GetByIdAsync(Guid id)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> LogAsync(CreateAdminAuditLogDto request)
            {
                this.MetadataJson = request.MetadataJson ?? string.Empty;
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(true, "Audit logged."));
            }
        }

        private sealed class FakePaymentProvider : IStorefrontPaymentProvider
        {
            public FakePaymentProvider(string providerKey)
            {
                this.ProviderKey = providerKey;
                this.Descriptor = new PaymentProviderDescriptor(
                    providerKey,
                    providerKey,
                    Description: null,
                    IconUrl: null,
                    DefaultDisplayOrder: 20,
                    SupportedCurrencyCodes: [],
                    SupportedCountryCodes: [],
                    MinOrderTotal: null,
                    MaxOrderTotal: null,
                    PaymentProviderMethodTypes.Redirect,
                    RecurringCapable: false,
                    SupportsAuthorize: false,
                    SupportsCapture: true,
                    SupportsVoid: false,
                    SupportsRefund: false,
                    SupportsPartialRefund: false,
                    RequiresWebhookSignature: true,
                    ActiveByDefault: false);
            }

            public string ProviderKey { get; }

            public PaymentProviderDescriptor Descriptor { get; }

            public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
