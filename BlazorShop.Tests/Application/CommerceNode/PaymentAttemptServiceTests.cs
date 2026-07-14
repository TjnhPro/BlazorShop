namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class PaymentAttemptServiceTests
    {
        [Fact]
        public async Task CreateAsync_DuplicateIdempotencyKey_ReturnsSameAttempt()
        {
            using var context = CreateContext();
            var service = new PaymentAttemptService(context);
            var request = CreateAttemptRequest(idempotencyKey: "same-key");

            var first = await service.CreateAsync(request);
            var second = await service.CreateAsync(request with { Amount = 999m });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.Payload!.Id, second.Payload!.Id);
            Assert.Equal(42m, second.Payload.Amount);
            Assert.Single(context.PaymentAttempts);
        }

        [Fact]
        public async Task GetAsync_ReturnsNamedAttemptState()
        {
            using var context = CreateContext();
            var service = new PaymentAttemptService(context);
            var created = await service.CreateAsync(CreateAttemptRequest(idempotencyKey: "poll-key"));

            var loaded = await service.GetAsync(created.Payload!.StoreId, created.Payload.Id);

            Assert.True(loaded.Success);
            Assert.Equal(created.Payload.Id, loaded.Payload!.Id);
            Assert.Equal(PaymentAttemptStates.Created, loaded.Payload.State);
        }

        [Fact]
        public async Task TransitionAsync_AllowsForwardState_AndRejectsTerminalTransition()
        {
            using var context = CreateContext();
            var service = new PaymentAttemptService(context);
            var created = await service.CreateAsync(CreateAttemptRequest(idempotencyKey: "transition-key"));

            var captured = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                created.Payload!.StoreId,
                created.Payload.Id,
                PaymentAttemptStates.Captured,
                ProviderReference: "cod-captured"));
            var rejected = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                created.Payload.StoreId,
                created.Payload.Id,
                PaymentAttemptStates.Authorized));

            Assert.True(captured.Success);
            Assert.Equal(PaymentAttemptStates.Captured, captured.Payload!.State);
            Assert.Equal("cod-captured", captured.Payload.ProviderReference);
            Assert.False(rejected.Success);
            Assert.Equal(ServiceResponseType.Conflict, rejected.ResponseType);
            Assert.Equal(PaymentAttemptStates.Captured, context.PaymentAttempts.Single().State);
        }

        [Fact]
        public async Task TransitionAsync_FailedProviderResultStoresSafeFailureDetails()
        {
            using var context = CreateContext();
            var service = new PaymentAttemptService(context);
            var created = await service.CreateAsync(CreateAttemptRequest(idempotencyKey: "failure-key"));

            var failed = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                created.Payload!.StoreId,
                created.Payload.Id,
                PaymentAttemptStates.Failed,
                FailureCode: "provider_declined",
                FailureMessage: "Payment was declined.",
                MetadataJson: "{\"safe\":true}"));

            Assert.True(failed.Success);
            Assert.Equal(PaymentAttemptStates.Failed, failed.Payload!.State);
            Assert.Equal("provider_declined", failed.Payload.FailureCode);
            Assert.Equal("Payment was declined.", failed.Payload.FailureMessage);
            Assert.Equal("{\"safe\":true}", context.PaymentAttempts.Single().MetadataJson);
        }

        [Fact]
        public async Task RecordProviderEventAsync_DeduplicatesByProviderEventId()
        {
            using var context = CreateContext();
            var service = new PaymentAttemptService(context);
            var request = new RecordPaymentProviderEventRequest(
                Guid.NewGuid(),
                PaymentAttemptId: null,
                ProviderKey: PaymentMethodKeys.Stripe,
                EventId: "evt_123",
                EventType: "checkout.session.completed",
                PayloadJson: "{\"id\":\"evt_123\"}");

            var first = await service.RecordProviderEventAsync(request);
            var second = await service.RecordProviderEventAsync(request with { PayloadJson = "{\"changed\":true}" });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.False(first.Payload!.IsDuplicate);
            Assert.True(second.Payload!.IsDuplicate);
            Assert.Equal(first.Payload.Id, second.Payload.Id);
            Assert.Single(context.PaymentProviderEvents);
            Assert.Equal(first.Payload.PayloadHash, second.Payload.PayloadHash);
        }

        private static CreatePaymentAttemptRequest CreateAttemptRequest(string idempotencyKey)
        {
            return new CreatePaymentAttemptRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                OrderId: null,
                PaymentMethodKeys.Cod,
                PaymentMethodKeys.Cod,
                42m,
                "usd",
                idempotencyKey,
                MetadataJson: "{\"mode\":\"test\"}");
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"payment-attempt-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
