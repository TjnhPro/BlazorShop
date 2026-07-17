namespace BlazorShop.Tests.Infrastructure.Services
{
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StorefrontCustomerServiceTests
    {
        [Fact]
        public async Task ResolveOrCreateAsync_CreatesCustomer_ForStoreEmail()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var storeId = Guid.NewGuid();

            var result = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                " Buyer@Example.COM ",
                " Buyer Name ",
                " 123456 "));

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.NotNull(result.Payload);
            Assert.Equal(storeId, result.Payload!.StoreId);
            Assert.Equal("buyer@example.com", result.Payload.Email);
            Assert.Equal("BUYER@EXAMPLE.COM", result.Payload.NormalizedEmail);
            Assert.Equal("Buyer Name", result.Payload.FullName);
            Assert.Equal("123456", result.Payload.Phone);
            Assert.Equal(1, await context.CommerceCustomers.CountAsync());
        }

        [Fact]
        public async Task ResolveOrCreateAsync_StoresProfileFields_WhenSupplied()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var storeId = Guid.NewGuid();

            var result = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                "buyer@example.com",
                Phone: " 123456 ",
                AppUserId: "user-1",
                FirstName: " Ada ",
                LastName: " Lovelace ",
                Company: " Analytical Engines ",
                PreferredLanguage: " en-US ",
                PreferredCurrencyCode: " usd "));

            Assert.True(result.Success);
            Assert.Equal("Ada Lovelace", result.Payload!.FullName);
            Assert.Equal("Ada", result.Payload.FirstName);
            Assert.Equal("Lovelace", result.Payload.LastName);
            Assert.Equal("Analytical Engines", result.Payload.Company);
            Assert.Equal("en-US", result.Payload.PreferredLanguage);
            Assert.Equal("USD", result.Payload.PreferredCurrencyCode);
            Assert.True(result.Payload.IsActive);
            Assert.Null(result.Payload.LastActivityAtUtc);
            var customer = await context.CommerceCustomers.SingleAsync();
            Assert.Equal("Ada", customer.FirstName);
            Assert.Equal("Lovelace", customer.LastName);
            Assert.Equal("Analytical Engines", customer.Company);
            Assert.Equal("en-US", customer.PreferredLanguage);
            Assert.Equal("USD", customer.PreferredCurrencyCode);
            Assert.True(customer.IsActive);
        }

        [Fact]
        public async Task ResolveOrCreateAsync_ReusesCustomer_ForSameStoreAndNormalizedEmail()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var storeId = Guid.NewGuid();

            var first = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                "buyer@example.com",
                "Buyer"));
            var second = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                " BUYER@example.com ",
                "Buyer Updated"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.Payload!.Id, second.Payload!.Id);
            Assert.Equal("Buyer Updated", second.Payload.FullName);
            Assert.Equal(1, await context.CommerceCustomers.CountAsync());
        }

        [Fact]
        public async Task ResolveOrCreateAsync_UpdatesProfileFields_WithoutBreakingFullNameFallback()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var storeId = Guid.NewGuid();
            await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                "buyer@example.com",
                "Buyer"));

            var result = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                "buyer@example.com",
                FirstName: "Grace",
                LastName: "Hopper",
                Company: "Compiler Lab",
                PreferredCurrencyCode: "eur"));

            Assert.True(result.Success);
            Assert.Equal("Grace Hopper", result.Payload!.FullName);
            Assert.Equal("Grace", result.Payload.FirstName);
            Assert.Equal("Hopper", result.Payload.LastName);
            Assert.Equal("Compiler Lab", result.Payload.Company);
            Assert.Equal("EUR", result.Payload.PreferredCurrencyCode);
            Assert.Equal("Grace Hopper", (await context.CommerceCustomers.SingleAsync()).FullName);
        }

        [Fact]
        public async Task ResolveOrCreateAsync_CreatesSeparateCustomers_ForSameEmailInDifferentStores()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var email = "buyer@example.com";

            var first = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                Guid.NewGuid(),
                email,
                "Buyer"));
            var second = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                Guid.NewGuid(),
                email,
                "Buyer"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.NotEqual(first.Payload!.Id, second.Payload!.Id);
            Assert.Equal(2, await context.CommerceCustomers.CountAsync());
        }

        [Fact]
        public async Task ResolveOrCreateAsync_LinksAuthenticatedUser_WhenAppUserIdProvided()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var storeId = Guid.NewGuid();

            var result = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                "buyer@example.com",
                "Buyer",
                AppUserId: "user-1"));

            Assert.True(result.Success);
            Assert.Equal("user-1", result.Payload!.AppUserId);
            Assert.Equal("user-1", (await context.CommerceCustomers.SingleAsync()).AppUserId);
        }

        [Fact]
        public async Task TouchLastActivityAsync_UpdatesAuthenticatedCustomerActivity()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);
            var storeId = Guid.NewGuid();
            await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                storeId,
                "buyer@example.com",
                "Buyer",
                AppUserId: "user-1"));
            var activityAt = new DateTimeOffset(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

            var result = await service.TouchLastActivityAsync(storeId, "user-1", activityAt);

            Assert.True(result.Success);
            Assert.Equal(activityAt, result.Payload!.LastActivityAtUtc);
            Assert.Equal(activityAt, (await context.CommerceCustomers.SingleAsync()).LastActivityAtUtc);
        }

        [Fact]
        public async Task ResolveOrCreateAsync_ReturnsValidationError_WhenEmailMissing()
        {
            await using var context = CreateContext();
            var service = new StorefrontCustomerService(context);

            var result = await service.ResolveOrCreateAsync(new StorefrontCustomerResolutionRequest(
                Guid.NewGuid(),
                " "));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal(0, await context.CommerceCustomers.CountAsync());
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-customer-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
