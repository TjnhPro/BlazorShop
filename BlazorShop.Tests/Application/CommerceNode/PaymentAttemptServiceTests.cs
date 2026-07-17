namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
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

            var failed = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                created.Payload!.StoreId,
                created.Payload.Id,
                PaymentAttemptStates.Failed,
                ProviderReference: "provider-ref",
                FailureCode: "provider_failed",
                FailureMessage: "Provider failed."));
            var rejected = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                created.Payload.StoreId,
                created.Payload.Id,
                PaymentAttemptStates.Authorized));

            Assert.True(failed.Success);
            Assert.Equal(PaymentAttemptStates.Failed, failed.Payload!.State);
            Assert.Equal("provider-ref", failed.Payload.ProviderReference);
            Assert.False(rejected.Success);
            Assert.Equal(ServiceResponseType.Conflict, rejected.ResponseType);
            Assert.Equal(PaymentAttemptStates.Failed, context.PaymentAttempts.Single().State);
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
        public async Task TransitionAsync_CapturedOnlineAttemptCreatesOrderExactlyOnce()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(storeId, price: 21m, stock: 5);
            context.Products.Add(product);
            var cart = new CartSession
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                TokenHash = "token",
                State = CartSessionStates.Active,
                Version = 2,
                Lines =
                {
                    new CartLine
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Quantity = 2,
                        UnitPriceSnapshot = 21m,
                        CurrencyCodeSnapshot = "USD",
                        LineKey = "line-1",
                    },
                },
            };
            var checkout = new CheckoutSession
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                CartSession = cart,
                CartSessionId = cart.Id,
                State = CheckoutSessionStates.OrderPending,
                CartVersion = 2,
                CustomerEmail = "customer@example.test",
                CustomerName = "Customer One",
                ShippingFullName = "Customer One",
                ShippingEmail = "customer@example.test",
                ShippingAddress1 = "100 Main St",
                ShippingCity = "New York",
                ShippingPostalCode = "10001",
                ShippingCountryCode = "US",
                PaymentMethodKey = PaymentMethodKeys.Stripe,
                GrandTotal = 42m,
                CurrencyCode = "USD",
            };
            var attempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                CheckoutSession = checkout,
                CheckoutSessionId = checkout.Id,
                PaymentMethodKey = PaymentMethodKeys.Stripe,
                ProviderKey = PaymentMethodKeys.Stripe,
                State = PaymentAttemptStates.RequiresAction,
                Amount = 42m,
                CurrencyCode = "USD",
                IdempotencyKey = "capture-key",
                ProviderSessionId = "cs_test",
            };
            context.CheckoutSessions.Add(checkout);
            context.PaymentAttempts.Add(attempt);
            context.SaveChanges();
            var service = new PaymentAttemptService(context);

            var first = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                storeId,
                attempt.PublicId,
                PaymentAttemptStates.Captured,
                ProviderReference: "pi_test",
                MetadataJson: "{\"event\":\"checkout.session.completed\"}"));
            var second = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                storeId,
                attempt.PublicId,
                PaymentAttemptStates.Captured,
                ProviderReference: "pi_test",
                MetadataJson: "{\"event\":\"checkout.session.completed\"}"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(PaymentAttemptStates.Captured, first.Payload!.State);
            Assert.Single(context.Orders);
            Assert.Single(context.OrderLines);
            Assert.Equal(context.Orders.Single().Id, first.Payload.OrderId);
            Assert.Equal(CartSessionStates.Ordered, context.CartSessions.Single().State);
            Assert.Equal(3, context.Products.Single(item => item.Id == product.Id).Quantity);
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

        [Fact]
        public async Task RecordProviderEventAsync_ResolvesAttemptByProviderSessionId()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var attempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                CheckoutSessionId = Guid.NewGuid(),
                PaymentMethodKey = PaymentMethodKeys.Stripe,
                ProviderKey = PaymentMethodKeys.Stripe,
                State = PaymentAttemptStates.RequiresAction,
                Amount = 42m,
                CurrencyCode = "USD",
                IdempotencyKey = "stripe-session-event",
                ProviderSessionId = "cs_test_123",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
            context.PaymentAttempts.Add(attempt);
            await context.SaveChangesAsync();
            var service = new PaymentAttemptService(context);

            var result = await service.RecordProviderEventAsync(new RecordPaymentProviderEventRequest(
                storeId,
                PaymentAttemptId: null,
                ProviderKey: PaymentMethodKeys.Stripe,
                EventId: "evt_session",
                EventType: "checkout.session.completed",
                PayloadJson: "{\"id\":\"evt_session\"}",
                ProviderSessionId: "cs_test_123"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(attempt.Id, result.Payload!.PaymentAttemptId);
            Assert.Equal(attempt.Id, context.PaymentProviderEvents.Single().PaymentAttemptId);
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

        private static Product CreatePublishedProduct(Guid storeId, decimal price, int stock)
        {
            var categoryId = Guid.NewGuid();
            return new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Name = "Published product",
                Slug = $"published-{Guid.NewGuid():N}",
                Price = price,
                Quantity = stock,
                IsPublished = true,
                PublishedOn = DateTime.UtcNow,
                ProductType = ProductTypes.Simple,
                CategoryId = categoryId,
                Category = new Category
                {
                    Id = categoryId,
                    StoreId = storeId,
                    Name = "Published category",
                    Slug = "published-category",
                    IsPublished = true,
                },
            };
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
