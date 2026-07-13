namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Domain.Constants;

    public sealed class CodPaymentHandler : IPaymentHandler
    {
        public string PaymentMethodKey => PaymentMethodKeys.Cod;

        public Task<PaymentHandlerResult> ProcessAsync(
            PaymentHandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentHandlerResult(
                true,
                "COD payment accepted.",
                PaymentStatuses.Paid,
                DateTime.UtcNow,
                MetadataJson: context.MetadataJson));
        }
    }

    public sealed class StripePaymentHandler : IPaymentHandler
    {
        public string PaymentMethodKey => PaymentMethodKeys.Stripe;

        public Task<PaymentHandlerResult> ProcessAsync(
            PaymentHandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentHandlerResult(
                false,
                "Stripe payment is not implemented yet.",
                PaymentStatuses.Pending));
        }
    }

    public sealed class PayPalPaymentHandler : IPaymentHandler
    {
        public string PaymentMethodKey => PaymentMethodKeys.PayPal;

        public Task<PaymentHandlerResult> ProcessAsync(
            PaymentHandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentHandlerResult(
                false,
                "PayPal payment is not implemented yet.",
                PaymentStatuses.Pending));
        }
    }

    public sealed class PaymentHandlerResolver : IPaymentHandlerResolver
    {
        private readonly IReadOnlyDictionary<string, IPaymentHandler> handlers;

        public PaymentHandlerResolver(IEnumerable<IPaymentHandler> handlers)
        {
            this.handlers = handlers.ToDictionary(
                handler => handler.PaymentMethodKey,
                StringComparer.OrdinalIgnoreCase);
        }

        public IPaymentHandler Resolve(string paymentMethodKey)
        {
            if (this.handlers.TryGetValue(paymentMethodKey, out var handler))
            {
                return handler;
            }

            throw new InvalidOperationException($"Payment method '{paymentMethodKey}' is not supported.");
        }
    }
}
