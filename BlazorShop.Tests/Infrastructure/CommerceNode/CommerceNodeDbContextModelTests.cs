namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    using Xunit;

    public sealed class CommerceNodeDbContextModelTests
    {
        [Theory]
        [InlineData(typeof(Product))]
        [InlineData(typeof(Category))]
        [InlineData(typeof(StoreSeoSettings))]
        [InlineData(typeof(StoreFeatureState))]
        [InlineData(typeof(StoreCurrency))]
        public void CatalogStoreId_IsRequiredInCommerceNode(Type entityType)
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(entityType);

            Assert.NotNull(modelEntity);
            var storeId = modelEntity!.FindProperty("StoreId");

            Assert.NotNull(storeId);
            Assert.False(storeId!.IsNullable);
        }

        [Theory]
        [InlineData(typeof(Product))]
        [InlineData(typeof(Category))]
        public void CatalogStoreForeignKey_RestrictsCommerceStoreDelete(Type entityType)
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(entityType);

            Assert.NotNull(modelEntity);
            var foreignKey = modelEntity!.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(foreignKey);
            Assert.True(foreignKey!.IsRequired);
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }

        [Fact]
        public void StoreSeoSettings_HasOneOverridePerStore()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreSeoSettings));

            Assert.NotNull(modelEntity);

            var storeIdIndex = modelEntity!.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId"]));
            var foreignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(storeIdIndex);
            Assert.True(storeIdIndex!.IsUnique);
            Assert.NotNull(foreignKey);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey!.DeleteBehavior);
        }

        [Fact]
        public void StoreFeatureState_HasOneStatePerFeaturePerStore()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreFeatureState));

            Assert.NotNull(modelEntity);

            var storeFeatureIndex = modelEntity!.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "FeatureKey"]));
            var foreignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(storeFeatureIndex);
            Assert.True(storeFeatureIndex!.IsUnique);
            Assert.NotNull(foreignKey);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey!.DeleteBehavior);
        }

        [Fact]
        public void StoreCurrency_HasOneCurrencyPerStore()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreCurrency));

            Assert.NotNull(modelEntity);

            var storeCurrencyIndex = modelEntity!.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "CurrencyCode"]));
            var foreignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(storeCurrencyIndex);
            Assert.True(storeCurrencyIndex!.IsUnique);
            Assert.NotNull(foreignKey);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey!.DeleteBehavior);
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5434;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=blazorshop_commerce_node_dev",
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(CommerceNodeDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure();
                    })
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
