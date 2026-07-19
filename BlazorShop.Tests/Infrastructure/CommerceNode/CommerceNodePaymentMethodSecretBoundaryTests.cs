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
            context.StorePaymentMethods.Add(new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = PaymentMethodKeys.PayPal,
                Enabled = false,
                DisplayName = "PayPal",
                DisplayOrder = 30,
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

        [Fact]
        public async Task GetAsync_CreatesMissingStoreMethodFromProviderDescriptor()
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
            var service = CreateService(
                context,
                storeId,
                providers:
                [
                    new FakePaymentProvider(
                        "bank_transfer",
                        "Bank Transfer",
                        PaymentProviderMethodTypes.Offline,
                        defaultDisplayOrder: 40,
                        enabledByDefault: true),
                ]);

            var methods = await service.GetAsync();

            var method = Assert.Single(methods, item => item.PaymentMethodKey == "bank_transfer");
            Assert.True(method.Enabled);
            Assert.Equal("Bank Transfer", method.DisplayName);
            Assert.Equal(40, method.DisplayOrder);
            Assert.Equal(PaymentProviderMethodTypes.Offline, method.Capability.MethodType);
            Assert.True(await context.StorePaymentMethods.AnyAsync(item => item.StoreId == storeId && item.PaymentMethodKey == "bank_transfer"));
        }

        [Fact]
        public async Task GetAsync_WhenProviderIsUnregistered_RetainsExistingMethodAsUnsupported()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            context.StorePaymentMethods.Add(new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = PaymentMethodKeys.PayPal,
                Enabled = false,
                DisplayName = "PayPal",
                DisplayOrder = 30,
            });
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var methods = await service.GetAsync();

            var paypal = Assert.Single(methods, item => item.PaymentMethodKey == PaymentMethodKeys.PayPal);
            Assert.False(paypal.Capability.Installed);
            Assert.False(paypal.Capability.Active);
            Assert.Equal("unknown", paypal.Capability.MethodType);
        }

        [Fact]
        public async Task GetAsync_DoesNotOverwriteCustomizedStoreMethodMetadata()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedConfiguredPaymentMethodAsync(
                context,
                storeId,
                method =>
                {
                    method.DisplayName = "My cards";
                    method.Description = "Custom copy.";
                    method.DisplayOrder = 7;
                    method.IconUrl = "/media/custom-card.svg";
                });
            var service = CreateService(context, storeId);

            await service.GetAsync();

            var method = await context.StorePaymentMethods.SingleAsync(item => item.StoreId == storeId && item.PaymentMethodKey == PaymentMethodKeys.Stripe);
            Assert.Equal("My cards", method.DisplayName);
            Assert.Equal("Custom copy.", method.Description);
            Assert.Equal(7, method.DisplayOrder);
            Assert.Equal("/media/custom-card.svg", method.IconUrl);
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
            IAdminAuditService? auditService = null,
            params IStorefrontPaymentProvider[] providers)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var providerList = providers.Length == 0
                ? [new FakePaymentProvider(PaymentMethodKeys.Stripe)]
                : providers;
            return new CommerceNodePaymentMethodService(
                context,
                new StubCommerceStoreContext(storeId),
                auditService ?? new CapturingAdminAuditService(),
                new StorefrontPublicConfigurationCache(context, cache),
                new PaymentProviderCapabilityRegistry(providerList));
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
            public FakePaymentProvider(
                string providerKey,
                string? displayName = null,
                string methodType = PaymentProviderMethodTypes.Redirect,
                int defaultDisplayOrder = 20,
                bool requiresWebhookSignature = true,
                bool activeByDefault = true,
                bool enabledByDefault = false)
            {
                this.ProviderKey = providerKey;
                this.Descriptor = new PaymentProviderDescriptor(
                    providerKey,
                    displayName ?? providerKey,
                    Description: null,
                    IconUrl: null,
                    defaultDisplayOrder,
                    SupportedCurrencyCodes: [],
                    SupportedCountryCodes: [],
                    MinOrderTotal: null,
                    MaxOrderTotal: null,
                    methodType,
                    RecurringCapable: false,
                    SupportsAuthorize: false,
                    SupportsCapture: true,
                    SupportsVoid: false,
                    SupportsRefund: false,
                    SupportsPartialRefund: false,
                    requiresWebhookSignature,
                    activeByDefault,
                    enabledByDefault);
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
