namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class OrderReadModelBehaviorLockTests
    {
        private const string AppUserId = "app-user-1";
        private const string GuestAccessToken = "guest-access-token";

        [Fact]
        public async Task AdminProjection_SeesAdminNoteAllHistoryAndCurrentPaymentSummary()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var service = new CommerceNodeAdminOrderService(
                context,
                new CommerceNodeOrderTrackingService(context, new FixedStoreContext(storeId)),
                new NoopAdminAuditService(),
                new FixedStoreContext(storeId),
                new OrderReadModelAssembler(context));

            var result = await service.GetByIdAsync(order.Id);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Manager-only note", result.Payload!.AdminNote);
            Assert.Equal(["public.event", "private.event"], result.Payload.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Empty(result.Payload.TrackingEvents);
            Assert.Equal("captured", result.Payload.PaymentSummary!.AttemptState);
            Assert.NotNull(result.Payload.PaymentSummary.PaymentAttemptPublicId);
            Assert.Equal(PaymentMethodKeys.Cod, result.Payload.PaymentSummary.ProviderKey);
            Assert.Equal("Fallback Product", Assert.Single(result.Payload.Lines).ProductName);
        }

        [Fact]
        public async Task CustomerProjection_HidesAdminNoteAndPrivateHistoryButKeepsTrackingEvents()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var service = new StorefrontCustomerOrderService(
                context,
                new FixedStoreContext(storeId),
                new OrderReadModelAssembler(context));

            var result = await service.GetReceiptAsync(new StorefrontCustomerOrderLookupRequest(AppUserId, order.Reference));

            Assert.True(result.Success, result.Message);
            Assert.Null(result.Payload!.AdminNote);
            Assert.Equal(["public.event"], result.Payload.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Equal("shipped", Assert.Single(result.Payload.TrackingEvents).Status);
            Assert.Equal("captured", result.Payload.PaymentSummary!.AttemptState);
            Assert.Null(result.Payload.PaymentSummary.PaymentAttemptPublicId);
            Assert.Null(result.Payload.PaymentSummary.ProviderKey);
            Assert.Equal("Fallback Product", Assert.Single(result.Payload.Lines).ProductName);
        }

        [Fact]
        public async Task GuestProjection_RequiresTokenAndStoreAndPreservesCurrentSafeVisibility()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var service = new StorefrontGuestOrderService(
                context,
                new FixedStoreContext(storeId),
                new OrderReadModelAssembler(context));
            var wrongStore = new StorefrontGuestOrderService(
                context,
                new FixedStoreContext(Guid.NewGuid()),
                new OrderReadModelAssembler(context));

            var result = await service.GetAsync(new StorefrontGuestOrderLookupRequest(order.Reference, GuestAccessToken));
            var invalidToken = await service.GetAsync(new StorefrontGuestOrderLookupRequest(order.Reference, "wrong-token"));
            var wrongStoreResult = await wrongStore.GetAsync(new StorefrontGuestOrderLookupRequest(order.Reference, GuestAccessToken));

            Assert.True(result.Success, result.Message);
            Assert.False(invalidToken.Success);
            Assert.Equal(ServiceResponseType.NotFound, invalidToken.ResponseType);
            Assert.False(wrongStoreResult.Success);
            Assert.Equal(ServiceResponseType.NotFound, wrongStoreResult.ResponseType);
            Assert.Null(result.Payload!.AdminNote);
            Assert.Equal(["public.event"], result.Payload.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Empty(result.Payload.TrackingEvents);
            Assert.Equal("captured", result.Payload.PaymentSummary!.AttemptState);
            Assert.NotNull(result.Payload.PaymentSummary.PaymentAttemptPublicId);
            Assert.Equal(PaymentMethodKeys.Cod, result.Payload.PaymentSummary.ProviderKey);
            Assert.Null(Assert.Single(result.Payload.Lines).ProductName);
        }

        [Fact]
        public async Task LegacyQueryProjection_CurrentlyUsesUserIdVisibleHistoryAndInternalPaymentFields()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var service = new CommerceNodeOrderQueryService(
                context,
                new FixedStoreContext(storeId),
                new OrderReadModelAssembler(context));

            var result = (await service.GetOrdersForUserAsync(AppUserId)).ToArray();

            var item = Assert.Single(result);
            Assert.Equal(order.Id, item.Id);
            Assert.Equal("Manager-only note", item.AdminNote);
            Assert.Equal(["public.event"], item.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Equal("shipped", Assert.Single(item.TrackingEvents).Status);
            Assert.Equal("captured", item.PaymentSummary!.AttemptState);
            Assert.NotNull(item.PaymentSummary.PaymentAttemptPublicId);
            Assert.Equal(PaymentMethodKeys.Cod, item.PaymentSummary.ProviderKey);
            Assert.Equal("Fallback Product", Assert.Single(item.Lines).ProductName);
        }

        [Fact]
        public async Task AssemblerAdminOptions_PreserveAdminProjectionShape()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var assembler = new OrderReadModelAssembler(context);

            var item = Assert.Single(await assembler.BuildAsync([order], OrderReadModelOptions.Admin()));
            var line = Assert.Single(item.Lines);

            Assert.Equal(AppUserId, item.UserId);
            Assert.Equal("Manager-only note", item.AdminNote);
            Assert.Equal(["public.event", "private.event"], item.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Empty(item.TrackingEvents);
            Assert.NotNull(item.PaymentSummary!.PaymentAttemptPublicId);
            Assert.Equal(PaymentMethodKeys.Cod, item.PaymentSummary.ProviderKey);
            Assert.Equal("Fallback Product", line.ProductName);
            Assert.Null(line.CurrencyCode);
            Assert.Null(line.PersistedLineTotal);
        }

        [Fact]
        public async Task AssemblerCustomerOptions_PreserveCustomerProjectionShape()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var assembler = new OrderReadModelAssembler(context);

            var item = Assert.Single(await assembler.BuildAsync([order], OrderReadModelOptions.Customer()));
            var line = Assert.Single(item.Lines);

            Assert.Null(item.UserId);
            Assert.Null(item.AdminNote);
            Assert.Equal(["public.event"], item.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Equal("shipped", Assert.Single(item.TrackingEvents).Status);
            Assert.Null(item.PaymentSummary!.PaymentAttemptPublicId);
            Assert.Null(item.PaymentSummary.ProviderKey);
            Assert.Equal("Fallback Product", line.ProductName);
            Assert.Equal("USD", line.CurrencyCode);
            Assert.Equal(25m, line.PersistedLineTotal);
        }

        [Fact]
        public async Task AssemblerGuestOptions_PreserveGuestProjectionShape()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var assembler = new OrderReadModelAssembler(context);

            var item = Assert.Single(await assembler.BuildAsync([order], OrderReadModelOptions.Guest()));
            var line = Assert.Single(item.Lines);

            Assert.Null(item.UserId);
            Assert.Null(item.AdminNote);
            Assert.Equal(["public.event"], item.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Empty(item.TrackingEvents);
            Assert.NotNull(item.PaymentSummary!.PaymentAttemptPublicId);
            Assert.Equal(PaymentMethodKeys.Cod, item.PaymentSummary.ProviderKey);
            Assert.Null(line.ProductName);
            Assert.Equal("USD", line.CurrencyCode);
            Assert.Equal(25m, line.PersistedLineTotal);
        }

        [Fact]
        public async Task AssemblerInternalOptions_PreserveLegacyQueryProjectionShape()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var order = SeedOrderGraph(context, storeId);
            var assembler = new OrderReadModelAssembler(context);

            var item = Assert.Single(await assembler.BuildAsync([order], OrderReadModelOptions.Internal()));

            Assert.Equal(AppUserId, item.UserId);
            Assert.Equal("Manager-only note", item.AdminNote);
            Assert.Equal(["public.event"], item.HistoryEntries.Select(entry => entry.EventType).ToArray());
            Assert.Equal("shipped", Assert.Single(item.TrackingEvents).Status);
            Assert.NotNull(item.PaymentSummary!.PaymentAttemptPublicId);
            Assert.Equal(PaymentMethodKeys.Cod, item.PaymentSummary.ProviderKey);
            Assert.Equal("Fallback Product", Assert.Single(item.Lines).ProductName);
        }

        private static Order SeedOrderGraph(CommerceNodeDbContext context, Guid storeId)
        {
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                AppUserId = AppUserId,
                Email = "buyer@example.test",
                NormalizedEmail = "BUYER@EXAMPLE.TEST",
                FullName = "Buyer One",
                IsActive = true,
            };
            context.CommerceCustomers.Add(customer);

            var productId = Guid.NewGuid();
            context.Products.Add(new Product
            {
                Id = productId,
                StoreId = storeId,
                Name = "Fallback Product",
                Slug = $"fallback-{Guid.NewGuid():N}",
                Price = 12.5m,
                IsPublished = true,
            });

            var order = new Order
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                CustomerId = customer.Id,
                UserId = AppUserId,
                Reference = $"ORD-{Guid.NewGuid():N}",
                OrderStatus = OrderStatuses.Processing,
                PaymentStatus = PaymentStatuses.Paid,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                PaymentAt = new DateTime(2026, 7, 19, 1, 0, 0, DateTimeKind.Utc),
                CurrencyCode = "USD",
                TotalAmount = 25m,
                CustomerEmail = customer.Email,
                CustomerName = customer.FullName,
                ShippingFullName = "Buyer One",
                ShippingEmail = customer.Email,
                ShippingAddress1 = "1 Checkout Street",
                ShippingCity = "Checkout City",
                ShippingPostalCode = "10000",
                ShippingCountryCode = "US",
                AdminNote = "Manager-only note",
                GuestAccessTokenHash = ComputeSha256(GuestAccessToken),
                GuestAccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                Lines =
                [
                    new OrderLine
                    {
                        ProductId = productId,
                        ProductName = null,
                        Quantity = 2,
                        UnitPrice = 12.5m,
                        CurrencyCode = "USD",
                        LineTotal = 25m,
                    },
                ],
            };
            context.Orders.Add(order);

            context.OrderHistoryEntries.AddRange(
                new OrderHistoryEntry
                {
                    StoreId = storeId,
                    OrderId = order.Id,
                    EventType = "public.event",
                    Message = "Public event.",
                    VisibleToCustomer = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
                    Source = "system",
                },
                new OrderHistoryEntry
                {
                    StoreId = storeId,
                    OrderId = order.Id,
                    EventType = "private.event",
                    Message = "Private event.",
                    VisibleToCustomer = false,
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Source = "admin",
                });

            context.PaymentAttempts.AddRange(
                new PaymentAttempt
                {
                    StoreId = storeId,
                    CheckoutSessionId = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethodKey = PaymentMethodKeys.Cod,
                    ProviderKey = PaymentMethodKeys.Cod,
                    State = "created",
                    Amount = 25m,
                    CurrencyCode = "USD",
                    IdempotencyKey = "older-attempt",
                    UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                },
                new PaymentAttempt
                {
                    StoreId = storeId,
                    CheckoutSessionId = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethodKey = PaymentMethodKeys.Cod,
                    ProviderKey = PaymentMethodKeys.Cod,
                    State = "captured",
                    Amount = 25m,
                    CurrencyCode = "USD",
                    IdempotencyKey = "latest-attempt",
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                });

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                OrderId = order.Id,
                ShipDate = DateTime.UtcNow,
                CarrierName = "UPS",
                TrackingNumber = "TRACK-1",
            };
            context.Shipments.Add(shipment);
            context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
            {
                ShipmentId = shipment.Id,
                StoreId = storeId,
                OrderId = order.Id,
                Status = "shipped",
                Message = "Shipment created.",
                Source = "manual_admin",
                OccurredAtUtc = DateTime.UtcNow,
            });

            context.SaveChanges();
            return order;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"order-read-model-lock-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static string ComputeSha256(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", this.storeId));
            }
        }

        private sealed class NoopAdminAuditService : IAdminAuditService
        {
            public Task<PagedResult<AdminAuditLogDto>> GetAsync(AdminAuditQueryDto query)
            {
                return Task.FromResult(new PagedResult<AdminAuditLogDto>());
            }

            public Task<ServiceResponse<AdminAuditLogDto>> GetByIdAsync(Guid id)
            {
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(false, "Not found.")
                {
                    ResponseType = ServiceResponseType.NotFound,
                });
            }

            public Task<ServiceResponse<AdminAuditLogDto>> LogAsync(CreateAdminAuditLogDto request)
            {
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(true, "Logged.")
                {
                    Payload = new AdminAuditLogDto(),
                    ResponseType = ServiceResponseType.Success,
                });
            }
        }
    }
}
