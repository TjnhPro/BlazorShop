namespace BlazorShop.Storefront.Presentation.Services.Checkout
{
    using BlazorShop.Storefront.Services.Contracts;
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
                    "rounded-3xl border border-amber-200 bg-amber-50 px-6 py-10 text-center shadow-sm",
                    "text-amber-700",
                    "text-amber-950",
                    "text-amber-900",
                    "text-amber-700",
                    isPending,
                    isSuccess,
                    true,
                    StorefrontLinkContext.Default);
            }

            var panelClass = isSuccess
                ? "rounded-3xl border border-emerald-200 bg-emerald-50 px-6 py-10 text-center shadow-sm"
                : isPending
                    ? "rounded-3xl border border-amber-200 bg-amber-50 px-6 py-10 text-center shadow-sm"
                    : "rounded-3xl border border-rose-200 bg-rose-50 px-6 py-10 text-center shadow-sm";

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
                panelClass,
                isSuccess
                    ? "text-emerald-700"
                    : isPending
                        ? "text-amber-700"
                        : "text-rose-700",
                isSuccess
                    ? "text-emerald-950"
                    : isPending
                        ? "text-amber-950"
                        : "text-rose-950",
                isSuccess
                    ? "text-emerald-900"
                    : isPending
                        ? "text-amber-900"
                        : "text-rose-900",
                isSuccess
                    ? "text-emerald-700"
                    : isPending
                        ? "text-amber-700"
                        : "text-rose-700",
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
