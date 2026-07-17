namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CommerceNodeAdminShipmentServiceTests
    {
        [Fact]
        public async Task UpsertShipmentAsync_WithoutItems_CreatesBackwardCompatibleFullOrderShipment()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var order = SeedOrder(context, storeId, quantity: 2);
            var service = CreateService(context, storeId);

            var result = await service.UpsertShipmentAsync(order.Id, CreateRequest());

            Assert.True(result.Success, result.Message);
            Assert.Empty(result.Payload!.Items);
            var trackingEvent = Assert.Single(result.Payload.TrackingEvents);
            Assert.Equal("shipped", trackingEvent.Status);
            Assert.Equal("manual_admin", trackingEvent.Source);
            Assert.Equal("Shipped", order.ShippingStatus);
            Assert.Equal("UPS", order.ShippingCarrier);
            Assert.Equal("TRACK-1", order.TrackingNumber);
            Assert.Equal("free_standard", result.Payload.ShippingMethod!.Key);
            Assert.Equal("internal", result.Payload.ShippingMethod.ProviderSystemName);
        }

        [Fact]
        public async Task UpsertShipmentAsync_WithItems_RejectsQuantityGreaterThanOrderedQuantity()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var order = SeedOrder(context, storeId, quantity: 2);
            var line = order.Lines.Single();
            var service = CreateService(context, storeId);

            var result = await service.UpsertShipmentAsync(
                order.Id,
                CreateRequest(items:
                [
                    new UpsertShipmentItemRequest
                    {
                        OrderLineId = line.Id,
                        ProductId = line.ProductId,
                        Quantity = 3,
                    },
                ]));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Empty(context.Shipments);
        }

        [Fact]
        public async Task UpsertShipmentAsync_WithItems_PersistsItemsAndTrackingEvent()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var order = SeedOrder(context, storeId, quantity: 2);
            var line = order.Lines.Single();
            var auditService = new CapturingAdminAuditService();
            var service = CreateService(context, storeId, auditService);

            var result = await service.UpsertShipmentAsync(
                order.Id,
                CreateRequest(items:
                [
                    new UpsertShipmentItemRequest
                    {
                        OrderLineId = line.Id,
                        ProductId = line.ProductId,
                        Quantity = 1,
                    },
                ]));

            Assert.True(result.Success, result.Message);
            var shipmentItem = Assert.Single(result.Payload!.Items);
            Assert.Equal(line.Id, shipmentItem.OrderLineId);
            Assert.Equal(1, shipmentItem.Quantity);
            Assert.Single(context.ShipmentItems);
            Assert.Single(context.ShipmentTrackingEvents);
            Assert.Equal("Order.ShipmentUpserted", Assert.Single(auditService.Logs).Action);
        }

        [Fact]
        public async Task UpsertShipmentAsync_WhenTrackingChanges_AppendsTrackingUpdatedEvent()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var order = SeedOrder(context, storeId, quantity: 1);
            var service = CreateService(context, storeId);

            await service.UpsertShipmentAsync(order.Id, CreateRequest());
            context.ChangeTracker.Clear();
            var result = await service.UpsertShipmentAsync(
                order.Id,
                CreateRequest(trackingNumber: "TRACK-2"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(["shipped", "tracking_updated"], result.Payload!.TrackingEvents.Select(item => item.Status).ToArray());
            Assert.Equal(2, context.ShipmentTrackingEvents.Count());
            Assert.Single(context.Shipments);
        }

        [Fact]
        public async Task UpdateShippingStatusAsync_WhenDelivered_AppendsDeliveredTrackingEvent()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var order = SeedOrder(context, storeId, quantity: 1);
            var shipmentService = CreateService(context, storeId);
            await shipmentService.UpsertShipmentAsync(order.Id, CreateRequest());
            context.ChangeTracker.Clear();
            var trackingService = new CommerceNodeOrderTrackingService(context, new StubCommerceStoreContext(storeId));
            var deliveredOn = new DateTime(2026, 7, 18, 2, 0, 0, DateTimeKind.Utc);

            var updated = await trackingService.UpdateShippingStatusAsync(order.Id, ShippingStatuses.Delivered, deliveredOn: deliveredOn);

            Assert.True(updated);
            Assert.Equal(
                ["shipped", "delivered"],
                context.ShipmentTrackingEvents.OrderBy(item => item.OccurredAtUtc).Select(item => item.Status).ToArray());
        }

        [Fact]
        public async Task OrderQueryService_ReturnsSafeTrackingEventsForStorefrontOrders()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var order = SeedOrder(context, storeId, quantity: 1);
            var shipmentService = CreateService(context, storeId);
            await shipmentService.UpsertShipmentAsync(order.Id, CreateRequest());
            context.ChangeTracker.Clear();
            var orderQueryService = new CommerceNodeOrderQueryService(context, new StubCommerceStoreContext(storeId));

            var orders = (await orderQueryService.GetOrdersForUserAsync("customer-1")).ToArray();

            var result = Assert.Single(orders);
            Assert.Equal(order.Id, result.Id);
            var trackingEvent = Assert.Single(result.TrackingEvents);
            Assert.Equal("shipped", trackingEvent.Status);
            Assert.Equal("Shipment created.", trackingEvent.Message);
            Assert.Equal("manual_admin", trackingEvent.Source);
        }

        private static UpsertShipmentRequest CreateRequest(
            string trackingNumber = "TRACK-1",
            IReadOnlyList<UpsertShipmentItemRequest>? items = null)
        {
            return new UpsertShipmentRequest
            {
                ShipDate = new DateTime(2026, 7, 17, 1, 0, 0, DateTimeKind.Utc),
                CarrierName = "UPS",
                CarrierService = "Ground",
                TrackingNumber = trackingNumber,
                TrackingUrl = $"https://tracking.example/{trackingNumber}",
                Items = items,
            };
        }

        private static CommerceNodeAdminShipmentService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            CapturingAdminAuditService? auditService = null)
        {
            return new CommerceNodeAdminShipmentService(
                context,
                auditService ?? new CapturingAdminAuditService(),
                new StubCommerceStoreContext(storeId));
        }

        private static void SeedStore(CommerceNodeDbContext context, Guid storeId)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            context.SaveChanges();
        }

        private static Order SeedOrder(CommerceNodeDbContext context, Guid storeId, int quantity)
        {
            var order = new Order
            {
                StoreId = storeId,
                UserId = "customer-1",
                Reference = $"ORD-{Guid.NewGuid():N}",
                CurrencyCode = "USD",
                TotalAmount = 20m,
                ShippingMethodKey = "free_standard",
                ShippingProviderSystemName = "internal",
                ShippingMethodCode = "standard",
                ShippingMethodName = "Free standard",
                ShippingTotal = 0m,
                ShippingCurrencyCode = "USD",
                ShippingDeliveryEstimateText = "3-5 business days",
                ShippingFullName = "Jane Customer",
                ShippingEmail = "jane@example.test",
                ShippingAddress1 = "10 Market St",
                ShippingCity = "Austin",
                ShippingPostalCode = "78701",
                ShippingCountryCode = "US",
            };

            order.Lines.Add(new OrderLine
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = quantity,
                UnitPrice = 10m,
                CurrencyCode = "USD",
            });

            context.Orders.Add(order);
            context.SaveChanges();

            return order;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-admin-shipments-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StubCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public StubCommerceStoreContext(Guid storeId)
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

        private sealed class CapturingAdminAuditService : IAdminAuditService
        {
            public List<CreateAdminAuditLogDto> Logs { get; } = [];

            public Task<PagedResult<AdminAuditLogDto>> GetAsync(AdminAuditQueryDto query)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> GetByIdAsync(Guid id)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> LogAsync(CreateAdminAuditLogDto request)
            {
                this.Logs.Add(request);
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(true, "Audit logged."));
            }
        }
    }
}
