namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class CheckoutSessionStateRulesTests
    {
        [Theory]
        [InlineData(CheckoutSessionStates.Draft, true)]
        [InlineData(CheckoutSessionStates.Ready, true)]
        [InlineData(CheckoutSessionStates.OrderPending, true)]
        [InlineData(CheckoutSessionStates.Completed, false)]
        [InlineData(CheckoutSessionStates.Expired, false)]
        [InlineData(CheckoutSessionStates.Cancelled, false)]
        public void IsActive_ReturnsCurrentCheckoutStatePolicy(string state, bool expected)
        {
            Assert.Equal(expected, CheckoutSessionStateRules.IsActive(state));
        }

        [Fact]
        public void MarkExpired_TransitionsToExpiredEntryAndIncrementsVersion()
        {
            var now = DateTimeOffset.UtcNow;
            var session = new CheckoutSession
            {
                State = CheckoutSessionStates.Ready,
                CurrentStep = CheckoutSteps.Review,
                CheckoutVersion = 4,
            };

            CheckoutSessionStateRules.MarkExpired(session, now);

            Assert.Equal(CheckoutSessionStates.Expired, session.State);
            Assert.Equal(CheckoutSteps.Entry, session.CurrentStep);
            Assert.Equal("expired", session.NextAction);
            Assert.Equal(5, session.CheckoutVersion);
            Assert.Equal(now, session.UpdatedAtUtc);
        }

        [Fact]
        public void Touch_UsesMinimumVersionAndUpdatesStateStepAndTimestamp()
        {
            var now = DateTimeOffset.UtcNow;
            var session = new CheckoutSession
            {
                CheckoutVersion = 0,
            };

            CheckoutSessionStateRules.Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.ShippingMethod, now);

            Assert.Equal(2, session.CheckoutVersion);
            Assert.Equal(CheckoutSessionStates.Draft, session.State);
            Assert.Equal(CheckoutSteps.ShippingMethod, session.CurrentStep);
            Assert.Equal(now, session.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(false, false, false, false, true, CheckoutSteps.BillingAddress)]
        [InlineData(true, false, false, false, true, CheckoutSteps.ShippingAddress)]
        [InlineData(true, true, false, false, true, CheckoutSteps.ShippingMethod)]
        [InlineData(true, true, true, false, true, CheckoutSteps.PaymentMethod)]
        [InlineData(true, true, true, true, true, CheckoutSteps.Review)]
        [InlineData(true, false, false, false, false, CheckoutSteps.PaymentMethod)]
        [InlineData(true, false, false, true, false, CheckoutSteps.Review)]
        public void ResolveNextRequiredStep_ReturnsCurrentStepPolicy(
            bool hasBillingAddress,
            bool hasShippingAddress,
            bool hasSelectedShippingOption,
            bool hasSelectedPaymentMethod,
            bool shippingRequired,
            string expected)
        {
            var result = CheckoutSessionStateRules.ResolveNextRequiredStep(
                hasBillingAddress,
                hasShippingAddress,
                hasSelectedShippingOption,
                hasSelectedPaymentMethod,
                shippingRequired);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ParseCompletedSteps_NormalizesKnownDistinctSteps()
        {
            var result = CheckoutSessionStateRules.ParseCompletedSteps("""["entry","entry","unknown","review"]""");

            Assert.Equal([CheckoutSteps.Entry, CheckoutSteps.Review], result);
        }
    }
}
