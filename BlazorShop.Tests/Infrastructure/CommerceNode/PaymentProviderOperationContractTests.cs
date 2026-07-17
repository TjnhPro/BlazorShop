namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class PaymentProviderOperationContractTests
    {
        [Fact]
        public async Task DefaultUnsupportedOperation_ReturnsTypedFailure()
        {
            IStorefrontPaymentProvider provider = new MinimalProvider();

            var result = await provider.RefundAsync(CreateOperationRequest());

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("payment.operation_not_supported", result.Payload!.SafeFailureCode);
        }

        [Fact]
        public async Task CodCreatePaymentSession_ReturnsSynchronousCapturedRecommendation()
        {
            var provider = new CodStorefrontPaymentProvider();

            var result = await provider.CreatePaymentSessionAsync(CreateSessionRequest(PaymentMethodKeys.Cod));

            Assert.True(result.Success, result.Message);
            Assert.Equal(PaymentProviderActionTypes.None, result.Payload!.ActionType);
            Assert.Equal("captured", result.Payload.RecommendedState);
            Assert.Contains(PaymentMethodKeys.Cod, result.Payload.MetadataJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CodCreateHostedSession_ReturnsUnsupportedFailure()
        {
            var provider = new CodStorefrontPaymentProvider();

            var result = await provider.CreateHostedSessionAsync(CreateSessionRequest(PaymentMethodKeys.Cod));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        private static PaymentProviderOperationRequest CreateOperationRequest()
        {
            return new PaymentProviderOperationRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                PaymentMethodKeys.Stripe,
                PaymentMethodKeys.Stripe,
                12.34m,
                "USD",
                "operation-key");
        }

        private static CreatePaymentProviderSessionRequest CreateSessionRequest(string providerKey)
        {
            return new CreatePaymentProviderSessionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                providerKey,
                providerKey,
                12.34m,
                "USD",
                "operation-key",
                [new PaymentProviderSessionLine(Guid.NewGuid(), "Product", 1, 12.34m)]);
        }

        private sealed class MinimalProvider : IStorefrontPaymentProvider
        {
            public string ProviderKey => "minimal";

            public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
