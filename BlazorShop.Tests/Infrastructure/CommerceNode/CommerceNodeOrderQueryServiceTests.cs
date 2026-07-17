namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CommerceNodeOrderQueryServiceTests
    {
        [Fact]
        public async Task GetOrdersForUserAsync_CurrentlyMissesV2OrdersLinkedOnlyByCustomerId()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                AppUserId = "app-user-1",
                Email = "buyer@example.test",
                NormalizedEmail = "BUYER@EXAMPLE.TEST",
                FullName = "Buyer One",
            };
            context.CommerceCustomers.Add(customer);
            context.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                CustomerId = customer.Id,
                UserId = string.Empty,
                Reference = "ORD-CUSTOMER-ONLY",
                OrderStatus = OrderStatuses.Processing,
                PaymentStatus = PaymentStatuses.Paid,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                CurrencyCode = "USD",
                TotalAmount = 10m,
                CustomerEmail = customer.Email,
                CustomerName = customer.FullName,
            });
            await context.SaveChangesAsync();
            var service = new CommerceNodeOrderQueryService(context, new FixedStoreContext(storeId));

            var orders = (await service.GetOrdersForUserAsync("app-user-1")).ToArray();

            Assert.Empty(orders);
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-order-query-{Guid.NewGuid():N}")
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
