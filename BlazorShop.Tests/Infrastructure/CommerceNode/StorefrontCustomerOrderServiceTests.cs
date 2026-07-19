namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StorefrontCustomerOrderServiceTests
    {
        [Fact]
        public async Task ListAsync_ReturnsV2OrdersLinkedByCustomerId()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var customer = SeedCustomer(context, storeId, "app-user-1", "buyer@example.test");
            var order = SeedOrder(context, storeId, "ORD-CUSTOMER", customer.Id, userId: string.Empty, email: customer.Email);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.ListAsync(new StorefrontCustomerOrderQuery("app-user-1", PageNumber: 1, PageSize: 10));

            Assert.True(result.Success, result.Message);
            var item = Assert.Single(result.Payload!.Items);
            Assert.Equal(order.Reference, item.Reference);
            Assert.Equal(1, result.Payload.TotalCount);
            Assert.Equal(1, result.Payload.PageNumber);
            Assert.Equal(10, result.Payload.PageSize);
        }

        [Fact]
        public async Task ListAsync_IncludesCompatibleLegacyUserIdOrdersOnly()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var customer = SeedCustomer(context, storeId, "app-user-1", "buyer@example.test");
            var legacy = SeedOrder(context, storeId, "ORD-LEGACY", customerId: null, userId: "app-user-1", email: " Buyer@Example.Test ");
            SeedOrder(context, storeId, "ORD-OTHER-EMAIL", customerId: null, userId: "app-user-1", email: "other@example.test");
            SeedOrder(context, storeId, "ORD-OTHER-CUSTOMER", Guid.NewGuid(), userId: "app-user-1", email: customer.Email);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.ListAsync(new StorefrontCustomerOrderQuery("app-user-1"));

            Assert.True(result.Success, result.Message);
            var item = Assert.Single(result.Payload!.Items);
            Assert.Equal(legacy.Reference, item.Reference);
        }

        [Fact]
        public async Task GetAsync_EnforcesCurrentCustomerOwnerCheck()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedCustomer(context, storeId, "app-user-1", "buyer@example.test");
            var otherCustomer = SeedCustomer(context, storeId, "app-user-2", "other@example.test");
            var otherOrder = SeedOrder(context, storeId, "ORD-OTHER", otherCustomer.Id, userId: "app-user-2", email: otherCustomer.Email);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.GetAsync(new StorefrontCustomerOrderLookupRequest("app-user-1", otherOrder.Reference));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
        }

        [Fact]
        public async Task GetReceiptAsync_ReturnsOwnedOrderWithSafeCustomerVisibleData()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var customer = SeedCustomer(context, storeId, "app-user-1", "buyer@example.test");
            var order = SeedOrder(context, storeId, "ORD-RECEIPT", customer.Id, userId: string.Empty, email: customer.Email);
            order.AdminNote = "Manager-only note";
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.GetReceiptAsync(new StorefrontCustomerOrderLookupRequest("app-user-1", order.Reference));

            Assert.True(result.Success, result.Message);
            Assert.Equal(order.Reference, result.Payload!.Reference);
            Assert.Null(result.Payload.AdminNote);
            Assert.Single(result.Payload.Lines);
        }

        private static StorefrontCustomerOrderService CreateService(CommerceNodeDbContext context, Guid storeId)
        {
            return new StorefrontCustomerOrderService(
                context,
                new FixedStoreContext(storeId),
                new OrderReadModelAssembler(context));
        }

        private static CommerceCustomer SeedCustomer(
            CommerceNodeDbContext context,
            Guid storeId,
            string appUserId,
            string email)
        {
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                AppUserId = appUserId,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                FullName = "Buyer",
                IsActive = true,
            };
            context.CommerceCustomers.Add(customer);
            return customer;
        }

        private static Order SeedOrder(
            CommerceNodeDbContext context,
            Guid storeId,
            string reference,
            Guid? customerId,
            string userId,
            string email)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                CustomerId = customerId,
                UserId = userId,
                Reference = reference,
                OrderStatus = OrderStatuses.Processing,
                PaymentStatus = PaymentStatuses.Paid,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                CurrencyCode = "USD",
                TotalAmount = 25m,
                CustomerEmail = email,
                CustomerName = "Buyer",
                ShippingFullName = "Buyer",
                ShippingEmail = email,
                ShippingAddress1 = "1 Checkout Street",
                ShippingCity = "Checkout City",
                ShippingPostalCode = "10000",
                ShippingCountryCode = "US",
                Lines =
                [
                    new OrderLine
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 12.5m,
                        LineTotal = 25m,
                    },
                ],
            };
            context.Orders.Add(order);
            return order;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-customer-order-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
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
    }
}
