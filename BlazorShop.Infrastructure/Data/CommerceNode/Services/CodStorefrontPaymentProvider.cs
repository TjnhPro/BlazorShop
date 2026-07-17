namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    public sealed class CodStorefrontPaymentProvider : IStorefrontPaymentProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public string ProviderKey => PaymentMethodKeys.Cod;

        public Task<ServiceResponse<PaymentProviderOperationResult>> ValidateInputAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0m)
            {
                return Task.FromResult(PaymentProviderOperationResult.Failed(
                    ServiceResponseType.ValidationError,
                    "Payment amount is invalid.",
                    "payment.amount_invalid"));
            }

            return Task.FromResult(PaymentProviderOperationResult.Succeeded("COD payment input accepted."));
        }

        public Task<ServiceResponse<PaymentProviderOperationResult>> CreatePaymentSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0m)
            {
                return Task.FromResult(PaymentProviderOperationResult.Failed(
                    ServiceResponseType.ValidationError,
                    "Payment amount is invalid.",
                    "payment.amount_invalid"));
            }

            var metadata = JsonSerializer.Serialize(new
            {
                provider = this.ProviderKey,
                paymentAttemptId = request.PaymentAttemptId,
                createdAt = DateTimeOffset.UtcNow,
            }, JsonOptions);

            return Task.FromResult(PaymentProviderOperationResult.Succeeded(
                "COD payment captured.",
                PaymentProviderActionTypes.None,
                metadataJson: metadata,
                recommendedState: PaymentAttemptStates.Captured));
        }

        public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ServiceResponse<PaymentProviderSessionResult>(
                false,
                "Payment operation 'create_hosted_session' is not supported.")
            {
                ResponseType = ServiceResponseType.ValidationError,
            });
        }
    }
}
