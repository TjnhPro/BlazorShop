namespace BlazorShop.Storefront.Presentation.Services.Checkout
{
    using BlazorShop.Storefront.Presentation.Contracts;
    using BlazorShop.Storefront.Runtime;
    using StorefrontPaymentAttemptResponse = BlazorShop.Storefront.Client.StorefrontPaymentAttemptResponse;

    public sealed class StorefrontPaymentResultPageService
    {
        private readonly IStorefrontRuntimePaymentFacade paymentFacade;

        public StorefrontPaymentResultPageService(IStorefrontRuntimePaymentFacade paymentFacade)
        {
            this.paymentFacade = paymentFacade;
        }

        public async Task<StorefrontPaymentResultPageContext> GetAsync(
            bool isCancelRoute,
            Guid? paymentAttemptId,
            string? provider,
            CancellationToken cancellationToken = default)
        {
            if (!paymentAttemptId.HasValue || paymentAttemptId.Value == Guid.Empty)
            {
                return CreateContext(
                    isCancelRoute,
                    paymentAttemptId,
                    provider,
                    attempt: null,
                    loadError: isCancelRoute
                        ? "Payment was cancelled before a provider status was available."
                        : "Payment status is not available.");
            }

            var result = await this.paymentFacade.GetAttemptAsync(paymentAttemptId.Value, cancellationToken);
            if (!result.Success || result.Value is null)
            {
                return CreateContext(
                    isCancelRoute,
                    paymentAttemptId,
                    provider,
                    attempt: null,
                    loadError: result.Error?.Status == StorefrontRuntimeStatusCodes.ServiceUnavailable
                        ? "Payment status is temporarily unavailable."
                        : "Payment status was not found.");
            }

            return CreateContext(isCancelRoute, paymentAttemptId, provider, ToPaymentAttemptView(result.Value), string.Empty);
        }

        private static StorefrontPaymentResultPageContext CreateContext(
            bool isCancelRoute,
            Guid? paymentAttemptId,
            string? provider,
            StorefrontPaymentResultAttemptView? attempt,
            string loadError)
        {
            var isSuccess = attempt is not null && IsSuccessState(attempt.State);
            var isPending = attempt is null && string.IsNullOrWhiteSpace(loadError)
                || attempt is not null && IsPendingState(attempt.State);
            var showRetry = !isSuccess;

            if (isCancelRoute)
            {
                return new StorefrontPaymentResultPageContext(
                    true,
                    paymentAttemptId,
                    provider,
                    attempt,
                    loadError,
                    attempt is null
                        ? "Payment cancelled"
                        : IsFailedState(attempt.State)
                            ? "Payment not completed"
                            : "Payment status",
                    attempt is null
                        ? "Checkout is still available"
                        : IsFailedState(attempt.State)
                            ? "Try another payment method"
                            : "Review payment status",
                    !string.IsNullOrWhiteSpace(loadError)
                        ? loadError
                        : attempt?.FailureMessage ?? "Your cart and checkout can be reviewed before trying payment again.",
                    StorefrontPaymentResultOutcome.Cancelled,
                    isPending,
                    isSuccess,
                    true,
                    StorefrontLinkContext.Default);
            }

            var outcome = isSuccess
                ? StorefrontPaymentResultOutcome.Success
                : isPending
                    ? StorefrontPaymentResultOutcome.Pending
                    : attempt is null && !string.IsNullOrWhiteSpace(loadError)
                        ? StorefrontPaymentResultOutcome.Unavailable
                        : StorefrontPaymentResultOutcome.Failed;

            return new StorefrontPaymentResultPageContext(
                false,
                paymentAttemptId,
                provider,
                attempt,
                loadError,
                isSuccess
                    ? "Payment confirmed"
                    : isPending
                        ? "Payment pending"
                        : "Payment needs attention",
                isSuccess
                    ? "Thank you"
                    : isPending
                        ? "Payment is being confirmed"
                        : "Payment was not completed",
                isSuccess
                    ? "Your payment is confirmed and the order is being prepared."
                    : isPending
                        ? "We are waiting for the payment provider to confirm this payment."
                        : string.IsNullOrWhiteSpace(loadError)
                            ? attempt?.FailureMessage ?? "Review the checkout and try another payment method."
                            : loadError,
                outcome,
                isPending,
                isSuccess,
                showRetry,
                StorefrontLinkContext.Default);
        }

        private static bool IsSuccessState(string? state)
        {
            return string.Equals(state, "captured", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "authorized", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPendingState(string? state)
        {
            return string.Equals(state, "created", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "requires_action", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFailedState(string? state)
        {
            return string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "cancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "expired", StringComparison.OrdinalIgnoreCase);
        }

        private static StorefrontPaymentResultAttemptView ToPaymentAttemptView(
            StorefrontPaymentAttemptResponse attempt)
        {
            return new StorefrontPaymentResultAttemptView(
                attempt.Id ?? Guid.Empty,
                attempt.State ?? string.Empty,
                attempt.FailureMessage);
        }
    }
}
