namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Moq;

    using Xunit;

    public sealed class PaymentAttemptServiceTests
    {
        [Fact]
        public void PaymentAndOrderPlacement_ConstructorsRequireDiAndDoNotBuildFallbackDependencies()
        {
            var paymentSource = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/PaymentAttemptService.cs");
            var placementSource = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/OrderPlacementService.cs");
            var paymentConstructor = Assert.Single(typeof(PaymentAttemptService).GetConstructors());
            var paymentParameters = paymentConstructor.GetParameters().ToDictionary(parameter => parameter.Name!, StringComparer.Ordinal);
            var placementConstructor = Assert.Single(typeof(OrderPlacementService).GetConstructors());
            var placementParameters = placementConstructor.GetParameters().ToDictionary(parameter => parameter.Name!, StringComparer.Ordinal);

            Assert.False(paymentParameters["orderPlacementService"].HasDefaultValue);
            Assert.False(paymentParameters["transactionalMessageService"].HasDefaultValue);
            Assert.Equal(typeof(IOrderPlacementService), paymentParameters["orderPlacementService"].ParameterType);
            Assert.Equal(typeof(ICommerceTransactionalMessageService), paymentParameters["transactionalMessageService"].ParameterType);
            Assert.DoesNotContain("IOrderPlacementService? orderPlacementService = null", paymentSource, StringComparison.Ordinal);
            Assert.DoesNotContain("ICommerceTransactionalMessageService? transactionalMessageService = null", paymentSource, StringComparison.Ordinal);
            Assert.DoesNotContain("orderPlacementService ?? new OrderPlacementService", paymentSource, StringComparison.Ordinal);

            Assert.False(placementParameters["moneyRoundingService"].HasDefaultValue);
            Assert.False(placementParameters["sellabilityResolver"].HasDefaultValue);
            Assert.False(placementParameters["stockAdjustmentHook"].HasDefaultValue);
            Assert.Equal(typeof(IMoneyRoundingService), placementParameters["moneyRoundingService"].ParameterType);
            Assert.Equal(typeof(IProductSellabilityResolver), placementParameters["sellabilityResolver"].ParameterType);
            Assert.Equal(typeof(IOrderStockAdjustmentHook), placementParameters["stockAdjustmentHook"].ParameterType);
            Assert.DoesNotContain("IMoneyRoundingService? moneyRoundingService = null", placementSource, StringComparison.Ordinal);
            Assert.DoesNotContain("IProductSellabilityResolver? sellabilityResolver = null", placementSource, StringComparison.Ordinal);
            Assert.DoesNotContain("IOrderStockAdjustmentHook? stockAdjustmentHook = null", placementSource, StringComparison.Ordinal);
            Assert.DoesNotContain("moneyRoundingService ?? new MoneyRoundingService", placementSource, StringComparison.Ordinal);
            Assert.DoesNotContain("sellabilityResolver ?? new ProductSellabilityResolver", placementSource, StringComparison.Ordinal);
            Assert.DoesNotContain("stockAdjustmentHook ?? new DefaultOrderStockAdjustmentHook", placementSource, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CreateAsync_DuplicateIdempotencyKey_ReturnsSameAttempt()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var request = CreateAttemptRequest(idempotencyKey: "same-key");

            var first = await service.CreateAsync(request);
            var second = await service.CreateAsync(request with { Amount = 999m });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.Payload!.Id, second.Payload!.Id);
            Assert.Equal(42m, second.Payload.Amount);
            Assert.Single(context.PaymentAttempts);
            var audit = Assert.Single(context.PaymentAttemptAuditLogs);
            Assert.Equal("payment_attempt.created", audit.EventType);
            Assert.Equal(PaymentAttemptStates.Created, audit.NewState);
        }

        [Fact]
        public async Task GetAsync_ReturnsNamedAttemptState()
        {
            using var context = CreateContext();
            var service = CreateService(context);
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
            var service = CreateService(context);
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
            Assert.Contains(context.PaymentAttemptAuditLogs, audit =>
                audit.EventType == "payment_attempt.failed"
                && audit.OldState == PaymentAttemptStates.Created
                && audit.NewState == PaymentAttemptStates.Failed);
        }

        [Fact]
        public async Task TransitionAsync_FailedProviderResultStoresSafeFailureDetails()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var created = await service.CreateAsync(CreateAttemptRequest(idempotencyKey: "failure-key"));

            var failed = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                created.Payload!.StoreId,
                created.Payload.Id,
                PaymentAttemptStates.Failed,
                FailureCode: "provider_declined",
                FailureMessage: "Payment was declined.",
                MetadataJson: "{\"secret\":\"sk_test_raw\",\"safe\":true}"));

            Assert.True(failed.Success);
            Assert.Equal(PaymentAttemptStates.Failed, failed.Payload!.State);
            Assert.Equal("provider_declined", failed.Payload.FailureCode);
            Assert.Equal("Payment was declined.", failed.Payload.FailureMessage);
            Assert.Equal("{\"secret\":\"sk_test_raw\",\"safe\":true}", context.PaymentAttempts.Single().MetadataJson);
            var audit = context.PaymentAttemptAuditLogs.Single(item => item.EventType == "payment_attempt.failed");
            Assert.Contains("provider_declined", audit.MetadataJson);
            Assert.DoesNotContain("sk_test_raw", audit.MetadataJson);
        }

        [Fact]
        public async Task TransitionAsync_WithOrderQueuesPaymentStatusNotificationWithoutBlockingStateChange()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            context.Orders.Add(new Order
            {
                Id = orderId,
                StoreId = storeId,
                UserId = "customer-1",
                Reference = "ORD-PAYMENT",
                CustomerEmail = "customer@example.test",
                CurrencyCode = "USD",
                TotalAmount = 42m,
            });
            await context.SaveChangesAsync();
            var notificationService = new Mock<ICommerceTransactionalMessageService>();
            notificationService
                .Setup(service => service.QueuePaymentStatusChangedAsync(
                    storeId,
                    orderId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueuedMessageResult(false, ErrorCode: "message_queue.failed", Message: "Queue unavailable."));
            var service = CreateService(context, transactionalMessageService: notificationService.Object);
            var created = await service.CreateAsync(new CreatePaymentAttemptRequest(
                storeId,
                Guid.NewGuid(),
                orderId,
                PaymentMethodKeys.Cod,
                PaymentMethodKeys.Cod,
                42m,
                "USD",
                "payment-notification-key"));

            var failed = await service.TransitionAsync(new TransitionPaymentAttemptRequest(
                storeId,
                created.Payload!.Id,
                PaymentAttemptStates.Failed,
                FailureCode: "provider_failed",
                FailureMessage: "Provider failed."));

            Assert.True(failed.Success);
            Assert.Equal(PaymentAttemptStates.Failed, failed.Payload!.State);
            notificationService.Verify(
                service => service.QueuePaymentStatusChangedAsync(
                    storeId,
                    orderId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
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
            var service = CreateService(context);

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
            var order = context.Orders.Include(item => item.Lines).Single();
            var line = Assert.Single(order.Lines);
            Assert.Equal(order.Id, first.Payload.OrderId);
            Assert.Equal(OrderStatuses.Processing, order.OrderStatus);
            Assert.Equal(PaymentStatuses.Paid, order.PaymentStatus);
            Assert.Equal("Customer One", order.CustomerName);
            Assert.Equal("customer@example.test", order.CustomerEmail);
            Assert.Equal(product.Name, line.ProductName);
            Assert.Equal(product.Sku, line.Sku);
            Assert.Equal(2, line.Quantity);
            Assert.Equal(21m, line.UnitPrice);
            Assert.Equal(42m, line.LineTotal);
            Assert.Equal("USD", line.CurrencyCode);
            Assert.Equal(CheckoutSessionStates.Completed, context.CheckoutSessions.Single().State);
            Assert.Equal(CartSessionStates.Ordered, context.CartSessions.Single().State);
            Assert.Equal(3, context.Products.Single(item => item.Id == product.Id).Quantity);
            Assert.Contains(context.PaymentAttemptAuditLogs, audit =>
                audit.EventType == "payment_attempt.captured"
                && audit.OldState == PaymentAttemptStates.RequiresAction
                && audit.NewState == PaymentAttemptStates.Captured
                && audit.OrderId == order.Id);
            Assert.Equal(
                ["order.created", "payment.captured"],
                context.OrderHistoryEntries.Select(item => item.EventType).OrderBy(item => item).ToArray());
            Assert.All(context.OrderHistoryEntries, item => Assert.True(item.VisibleToCustomer));
            Assert.Single(context.CommerceTasks.Where(item => item.TaskType == OrderPlacementTaskTypes.OrderCreated));
        }

        [Fact]
        public async Task RecordProviderEventAsync_DeduplicatesByProviderEventId()
        {
            using var context = CreateContext();
            var service = CreateService(context);
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
            var service = CreateService(context);

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

        private static PaymentAttemptService CreateService(
            CommerceNodeDbContext context,
            ICommerceTransactionalMessageService? transactionalMessageService = null,
            IOrderPlacementService? orderPlacementService = null)
        {
            var roundingService = new MoneyRoundingService(new CurrencyMetadataService());
            var placementService = orderPlacementService
                ?? new OrderPlacementService(
                    context,
                    roundingService,
                    new CheckoutOrderLineResolver(
                        context,
                        roundingService,
                        new ProductSellabilityResolver()),
                    new DefaultOrderStockAdjustmentHook());
            var messageService = transactionalMessageService
                ?? new Mock<ICommerceTransactionalMessageService>().Object;

            return new PaymentAttemptService(context, placementService, messageService);
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

        private static string ReadRepositoryFile(string relativePath)
        {
            var root = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(root) && !File.Exists(Path.Combine(root, "BlazorShop.sln")))
            {
                root = Directory.GetParent(root)?.FullName ?? string.Empty;
            }

            return File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
