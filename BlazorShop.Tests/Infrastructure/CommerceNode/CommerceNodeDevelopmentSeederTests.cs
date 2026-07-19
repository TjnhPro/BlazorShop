namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    using Xunit;

    public sealed class CommerceNodeDevelopmentSeederTests
    {
        private static readonly Guid ApparelCategoryId = Guid.Parse("8d4830f9-a21f-4f4a-96d7-83d1e6dc0201");
        private static readonly Guid SimpleProductId = Guid.Parse("2b111111-1111-4111-8111-111111111101");

        [Fact]
        public async Task SeedAsync_WhenQaSeedAlreadyExists_DoesNotResetStoreRuntimeProfile()
        {
            await using var context = CreateDbContext();
            var storeId = Guid.NewGuid();
            var store = new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Operator Edited Store",
                Status = CommerceStoreStatuses.Active,
                BaseUrl = "https://edited-store.example",
                LogoUrl = "/operator-logo.png",
                FaviconUrl = "/operator-favicon.ico",
                PngIconUrl = "/operator-icon.png",
                DefaultCurrencyCode = "USD",
                DefaultCulture = "vi-VN",
                MaintenanceModeEnabled = true,
                MaintenanceMessage = "Operator maintenance message.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            };
            context.CommerceStores.Add(store);
            context.Categories.Add(new Category
            {
                Id = ApparelCategoryId,
                StoreId = storeId,
                Name = "Existing QA Category",
                Slug = "existing-qa-category",
            });
            context.Products.Add(new Product
            {
                Id = SimpleProductId,
                StoreId = storeId,
                CategoryId = ApparelCategoryId,
                Name = "Existing QA Product",
                Slug = "existing-qa-product",
                Price = 12.34m,
                Quantity = 5,
            });
            await context.SaveChangesAsync();

            var seeder = new CommerceNodeDevelopmentSeeder(
                context,
                userManager: null!,
                hostEnvironment: null!,
                mediaStorageProvider: null!,
                Options.Create(new CommerceMediaStorageOptions()));

            await seeder.SeedAsync();

            var persisted = await context.CommerceStores.SingleAsync(candidate => candidate.StoreKey == "default");
            Assert.Equal("Operator Edited Store", persisted.Name);
            Assert.Equal("https://edited-store.example", persisted.BaseUrl);
            Assert.Equal("/operator-logo.png", persisted.LogoUrl);
            Assert.Equal("/operator-favicon.ico", persisted.FaviconUrl);
            Assert.Equal("/operator-icon.png", persisted.PngIconUrl);
            Assert.Equal("USD", persisted.DefaultCurrencyCode);
            Assert.Equal("vi-VN", persisted.DefaultCulture);
            Assert.True(persisted.MaintenanceModeEnabled);
            Assert.Equal("Operator maintenance message.", persisted.MaintenanceMessage);
            Assert.Empty(context.StorePaymentMethods);
        }

        private static CommerceNodeDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-development-seeder-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
