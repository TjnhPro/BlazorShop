namespace BlazorShop.Tests.Infrastructure.Services
{
    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StorefrontCustomerAddressServiceTests
    {
        [Fact]
        public async Task CreateAsync_CreatesCustomerAddressAndDefaults_WhenCustomerIsMissing()
        {
            await using var context = CreateContext();
            var service = CreateService(context);
            var storeId = Guid.NewGuid();

            var result = await service.CreateAsync(
                CustomerContext(storeId, "customer-1", " Buyer@Example.COM "),
                ValidAddress() with
                {
                    FirstName = " Ada ",
                    CountryCode = " us ",
                    StateProvinceCode = " ny ",
                    IsDefaultShipping = false,
                    IsDefaultBilling = false,
                });

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.NotNull(result.Payload);
            Assert.Equal("Ada", result.Payload!.FirstName);
            Assert.Equal("US", result.Payload.CountryCode);
            Assert.Equal("NY", result.Payload.StateProvinceCode);
            Assert.True(result.Payload.IsDefaultShipping);
            Assert.True(result.Payload.IsDefaultBilling);

            var customer = await context.CommerceCustomers.SingleAsync();
            Assert.Equal(storeId, customer.StoreId);
            Assert.Equal("customer-1", customer.AppUserId);

            var address = await context.CommerceCustomerAddresses.SingleAsync();
            Assert.Equal(customer.Id, address.CustomerId);
            Assert.Equal(storeId, address.StoreId);
        }

        [Fact]
        public async Task CreateAsync_WhenNewShippingDefault_ClearsPreviousShippingDefault()
        {
            await using var context = CreateContext();
            var service = CreateService(context);
            var storeId = Guid.NewGuid();
            var customer = CustomerContext(storeId, "customer-1", "buyer@example.test");

            var first = await service.CreateAsync(customer, ValidAddress() with { IsDefaultShipping = true });
            var second = await service.CreateAsync(customer, ValidAddress() with
            {
                Address1 = "200 Main St",
                IsDefaultShipping = true,
                IsDefaultBilling = false,
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            var addresses = await context.CommerceCustomerAddresses
                .OrderBy(address => address.Address1)
                .ToArrayAsync();

            Assert.Equal(2, addresses.Length);
            Assert.Single(addresses, address => address.IsDefaultShipping);
            Assert.Equal("200 Main St", addresses.Single(address => address.IsDefaultShipping).Address1);
            Assert.Single(addresses, address => address.IsDefaultBilling);
        }

        [Fact]
        public async Task UpdateAsync_WhenAddressBelongsToAnotherCustomer_ReturnsNotFound()
        {
            await using var context = CreateContext();
            var service = CreateService(context);
            var storeId = Guid.NewGuid();

            var created = await service.CreateAsync(
                CustomerContext(storeId, "customer-1", "one@example.test"),
                ValidAddress());

            var result = await service.UpdateAsync(
                CustomerContext(storeId, "customer-2", "two@example.test"),
                created.Payload!.PublicId,
                ValidAddress() with { FirstName = "Mallory" });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal("Ada", (await context.CommerceCustomerAddresses.SingleAsync()).FirstName);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesAddressAndListExcludesIt()
        {
            await using var context = CreateContext();
            var service = CreateService(context);
            var customer = CustomerContext(Guid.NewGuid(), "customer-1", "buyer@example.test");
            var created = await service.CreateAsync(customer, ValidAddress());

            var delete = await service.DeleteAsync(customer, created.Payload!.PublicId);
            var list = await service.ListAsync(customer);

            Assert.True(delete.Success);
            Assert.NotNull((await context.CommerceCustomerAddresses.SingleAsync()).DeletedAtUtc);
            Assert.Empty(list.Payload!);
        }

        [Fact]
        public async Task SetDefaultBillingAsync_ClearsOtherBillingDefaults()
        {
            await using var context = CreateContext();
            var service = CreateService(context);
            var customer = CustomerContext(Guid.NewGuid(), "customer-1", "buyer@example.test");

            var first = await service.CreateAsync(customer, ValidAddress() with { IsDefaultBilling = true });
            var second = await service.CreateAsync(customer, ValidAddress() with
            {
                Address1 = "200 Main St",
                IsDefaultShipping = false,
                IsDefaultBilling = false,
            });

            var result = await service.SetDefaultBillingAsync(customer, second.Payload!.PublicId);

            Assert.True(first.Success);
            Assert.True(result.Success);
            var addresses = await context.CommerceCustomerAddresses.ToArrayAsync();
            Assert.Single(addresses, address => address.IsDefaultBilling);
            Assert.Equal(second.Payload.PublicId, addresses.Single(address => address.IsDefaultBilling).PublicId);
        }

        private static StorefrontCustomerAddressContext CustomerContext(
            Guid storeId,
            string appUserId,
            string email)
        {
            return new StorefrontCustomerAddressContext(storeId, appUserId, email, "Buyer Name");
        }

        private static CustomerAddressCreateRequest ValidAddress()
        {
            return new CustomerAddressCreateRequest(
                "Ada",
                "Lovelace",
                null,
                "100 Main St",
                null,
                "New York",
                "10001",
                "US",
                "NY",
                "New York",
                "5550100",
                "ada@example.test",
                IsDefaultShipping: false,
                IsDefaultBilling: false);
        }

        private static StorefrontCustomerAddressService CreateService(CommerceNodeDbContext context)
        {
            return new StorefrontCustomerAddressService(
                context,
                new StorefrontCustomerService(context),
                new AddressValidationService());
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-customer-address-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
