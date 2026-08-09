extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Presentation.Services.Checkout;

    public sealed class StorefrontPaymentResultPageServiceTests
    {
        [Fact]
        public async Task GetAsync_ReturnsCancelledOutcomeForCancelRouteWithoutAttempt()
        {
            var context = await CreateService(StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>.Failed(NotFound()))
                .GetAsync(isCancelRoute: true, paymentAttemptId: null, provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Cancelled, context.Outcome);
            Assert.False(context.IsSuccess);
            Assert.False(context.IsPending);
            Assert.True(context.ShowRetry);
            Assert.Equal("Payment cancelled", context.Eyebrow);
        }

        [Fact]
        public async Task GetAsync_ReturnsUnavailableOutcomeWhenAttemptIdIsMissingOnResultRoute()
        {
            var context = await CreateService(StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>.Failed(NotFound()))
                .GetAsync(isCancelRoute: false, paymentAttemptId: null, provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Unavailable, context.Outcome);
            Assert.False(context.IsSuccess);
            Assert.False(context.IsPending);
            Assert.True(context.ShowRetry);
            Assert.Equal("Payment status is not available.", context.Body);
        }

        [Theory]
        [InlineData("captured")]
        [InlineData("authorized")]
        public async Task GetAsync_ReturnsSuccessOutcomeForSuccessfulAttemptStates(string state)
        {
            var context = await CreateService(SucceededAttempt(state))
                .GetAsync(isCancelRoute: false, paymentAttemptId: Guid.NewGuid(), provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Success, context.Outcome);
            Assert.True(context.IsSuccess);
            Assert.False(context.IsPending);
            Assert.False(context.ShowRetry);
            Assert.Equal("Payment confirmed", context.Eyebrow);
        }

        [Theory]
        [InlineData("created")]
        [InlineData("requires_action")]
        public async Task GetAsync_ReturnsPendingOutcomeForPendingAttemptStates(string state)
        {
            var context = await CreateService(SucceededAttempt(state))
                .GetAsync(isCancelRoute: false, paymentAttemptId: Guid.NewGuid(), provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Pending, context.Outcome);
            Assert.False(context.IsSuccess);
            Assert.True(context.IsPending);
            Assert.True(context.ShowRetry);
            Assert.Equal("Payment pending", context.Eyebrow);
        }

        [Fact]
        public async Task GetAsync_ReturnsFailedOutcomeForFailedAttemptState()
        {
            var context = await CreateService(SucceededAttempt("failed", "Card declined."))
                .GetAsync(isCancelRoute: false, paymentAttemptId: Guid.NewGuid(), provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Failed, context.Outcome);
            Assert.False(context.IsSuccess);
            Assert.False(context.IsPending);
            Assert.True(context.ShowRetry);
            Assert.Equal("Card declined.", context.Body);
        }

        [Fact]
        public async Task GetAsync_PreservesCancelledOutcomeForCancelRouteWithFailedAttempt()
        {
            var context = await CreateService(SucceededAttempt("failed", "Card declined."))
                .GetAsync(isCancelRoute: true, paymentAttemptId: Guid.NewGuid(), provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Cancelled, context.Outcome);
            Assert.False(context.IsSuccess);
            Assert.False(context.IsPending);
            Assert.True(context.ShowRetry);
            Assert.Equal("Payment not completed", context.Eyebrow);
            Assert.Equal("Card declined.", context.Body);
        }

        [Fact]
        public async Task GetAsync_ReturnsUnavailableOutcomeWhenAttemptLoadFails()
        {
            var context = await CreateService(StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>.Failed(Unavailable()))
                .GetAsync(isCancelRoute: false, paymentAttemptId: Guid.NewGuid(), provider: "test");

            Assert.Equal(StorefrontPaymentResultOutcome.Unavailable, context.Outcome);
            Assert.False(context.IsSuccess);
            Assert.False(context.IsPending);
            Assert.True(context.ShowRetry);
            Assert.Equal("Payment status is temporarily unavailable.", context.Body);
        }

        private static StorefrontPaymentResultPageService CreateService(
            StorefrontRuntimeResult<StorefrontPaymentAttemptResponse> result)
        {
            return new StorefrontPaymentResultPageService(new StubPaymentFacade(result));
        }

        private static StorefrontRuntimeResult<StorefrontPaymentAttemptResponse> SucceededAttempt(
            string state,
            string? failureMessage = null)
        {
            return StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>.Succeeded(new StorefrontPaymentAttemptResponse
            {
                Id = Guid.NewGuid(),
                State = state,
                FailureMessage = failureMessage,
            });
        }

        private static StorefrontRuntimeError NotFound()
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.NotFound,
                "http.404",
                "Not found.",
                null,
                EmptyFieldErrors());
        }

        private static StorefrontRuntimeError Unavailable()
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                "storefront.unavailable",
                "Unavailable.",
                null,
                EmptyFieldErrors());
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyFieldErrors()
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        private sealed class StubPaymentFacade : IStorefrontRuntimePaymentFacade
        {
            private readonly StorefrontRuntimeResult<StorefrontPaymentAttemptResponse> result;

            public StubPaymentFacade(StorefrontRuntimeResult<StorefrontPaymentAttemptResponse> result)
            {
                this.result = result;
            }

            public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontPaymentMethodResponse>>> ListMethodsAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<StorefrontPaymentMethodResponse>>.Succeeded([]));
            }

            public Task<StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>> GetAttemptAsync(
                Guid paymentAttemptId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.result);
            }
        }
    }
}
