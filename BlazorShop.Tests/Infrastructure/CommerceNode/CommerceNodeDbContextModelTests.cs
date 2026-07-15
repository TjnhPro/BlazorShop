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
