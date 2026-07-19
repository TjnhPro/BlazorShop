namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class PaymentProviderCapabilityRegistryTests
    {
        [Fact]
        public void List_ReturnsCapabilitiesFromProviderDescriptors()
        {
            var registry = new PaymentProviderCapabilityRegistry(
            [
                new FakePaymentProvider("bank_transfer", "Bank Transfer", PaymentProviderMethodTypes.Offline, 30, requiresWebhookSignature: false),
                new FakePaymentProvider(PaymentMethodKeys.Stripe, "Stripe", PaymentProviderMethodTypes.Redirect, 20, requiresWebhookSignature: true),
            ]);

            var capabilities = registry.List();

            Assert.Equal([PaymentMethodKeys.Stripe, "bank_transfer"], capabilities.Select(capability => capability.SystemName).ToArray());
            var bankTransfer = Assert.Single(capabilities, item => item.SystemName == "bank_transfer");
            Assert.True(bankTransfer.Installed);
            Assert.True(bankTransfer.Active);
            Assert.Equal("Bank Transfer", bankTransfer.DisplayName);
            Assert.Equal(PaymentProviderMethodTypes.Offline, bankTransfer.MethodType);
            Assert.False(bankTransfer.RequiresWebhookSignature);

            var stripe = Assert.Single(capabilities, item => item.SystemName == PaymentMethodKeys.Stripe);
            Assert.True(stripe.Installed);
            Assert.True(stripe.Active);
            Assert.Equal(PaymentProviderMethodTypes.Redirect, stripe.MethodType);
            Assert.True(stripe.RequiresWebhookSignature);
        }

        [Fact]
        public void List_DoesNotReturnPayPalUnlessProviderIsRegistered()
        {
            var registry = new PaymentProviderCapabilityRegistry([new FakePaymentProvider(PaymentMethodKeys.Stripe)]);

            var capabilities = registry.List();

            Assert.DoesNotContain(capabilities, item => item.SystemName == PaymentMethodKeys.PayPal);
        }

        [Fact]
        public void Get_WhenProviderUnknown_ReturnsValidationFailure()
        {
            var registry = new PaymentProviderCapabilityRegistry([]);

            var result = registry.Get("bank_transfer");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Payment provider is not supported.", result.Message);
        }

        [Fact]
        public void Constructor_WhenProviderKeysAreDuplicated_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PaymentProviderCapabilityRegistry(
            [
                new FakePaymentProvider(PaymentMethodKeys.Stripe),
                new FakePaymentProvider(PaymentMethodKeys.Stripe.ToUpperInvariant()),
            ]));
        }

        [Fact]
        public void Constructor_WhenProviderKeyDoesNotMatchDescriptor_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PaymentProviderCapabilityRegistry(
            [
                new FakePaymentProvider(PaymentMethodKeys.Stripe, descriptorSystemName: "stripe_mismatch"),
            ]));
        }

        [Fact]
        public void RegistrySource_DoesNotContainProviderSpecificFactories()
        {
            var source = File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "BlazorShop.Infrastructure",
                "Data",
                "CommerceNode",
                "Services",
                "PaymentProviderCapabilityRegistry.cs"));

            Assert.DoesNotContain("CreateCod", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CreateStripe", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CreatePayPalSkeleton", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PaymentMethodKeys.Cod", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PaymentMethodKeys.Stripe", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PaymentMethodKeys.PayPal", source, StringComparison.Ordinal);
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
                string? descriptorSystemName = null)
            {
                this.ProviderKey = providerKey;
                this.Descriptor = new PaymentProviderDescriptor(
                    descriptorSystemName ?? providerKey,
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
                    activeByDefault);
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
