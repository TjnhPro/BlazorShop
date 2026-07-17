namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;

    using Microsoft.Extensions.Configuration;

    public sealed class PaymentWebhookSignatureVerifier : IPaymentWebhookSignatureVerifier
    {
        private readonly IPaymentProviderCapabilityRegistry capabilityRegistry;
        private readonly IConfiguration configuration;

        public PaymentWebhookSignatureVerifier(
            IPaymentProviderCapabilityRegistry capabilityRegistry,
            IConfiguration configuration)
        {
            this.capabilityRegistry = capabilityRegistry;
            this.configuration = configuration;
        }

        public Task<ServiceResponse<object?>> VerifyAsync(
            string providerKey,
            string payloadJson,
            string? providerSignature,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            var capability = this.capabilityRegistry.Get(providerKey);
            if (!capability.Success || capability.Payload is null)
            {
                return Task.FromResult(Failed(ServiceResponseType.Conflict, "Payment provider is not supported."));
            }

            if (!capability.Payload.RequiresWebhookSignature)
            {
                return Task.FromResult(Succeeded("Payment webhook signature is not required."));
            }

            if (string.IsNullOrWhiteSpace(providerSignature))
            {
                return Task.FromResult(Failed(ServiceResponseType.ValidationError, "Payment provider signature is required."));
            }

            var secret = this.ResolveWebhookSecret(providerKey);
            if (string.IsNullOrWhiteSpace(secret))
            {
                return Task.FromResult(Failed(ServiceResponseType.Conflict, "Payment provider webhook signature is not configured."));
            }

            var expected = ComputeHmacSha256(payloadJson ?? string.Empty, secret);
            var normalizedSignature = NormalizeSignature(providerSignature);
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(normalizedSignature)))
            {
                return Task.FromResult(Failed(ServiceResponseType.ValidationError, "Payment provider signature is invalid."));
            }

            return Task.FromResult(Succeeded("Payment webhook signature verified."));
        }

        private string? ResolveWebhookSecret(string providerKey)
        {
            var normalized = providerKey.Trim().ToLowerInvariant();
            var configured = this.configuration[$"PaymentProviders:{normalized}:WebhookSecret"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return string.Equals(normalized, PaymentMethodKeys.Stripe, StringComparison.OrdinalIgnoreCase)
                ? this.configuration["Stripe:WebhookSecret"]
                : null;
        }

        private static string NormalizeSignature(string value)
        {
            var normalized = value.Trim();
            if (normalized.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            {
                return normalized["sha256=".Length..].Trim();
            }

            if (normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                return normalized["sha256:".Length..].Trim();
            }

            return normalized;
        }

        private static string ComputeHmacSha256(string payloadJson, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
        }

        private static ServiceResponse<object?> Succeeded(string message)
        {
            return new ServiceResponse<object?>(true, message)
            {
                Payload = null,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<object?> Failed(ServiceResponseType responseType, string message)
        {
            return new ServiceResponse<object?>(false, message)
            {
                Payload = null,
                ResponseType = responseType,
            };
        }
    }
}
