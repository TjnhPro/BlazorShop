namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Domain.Constants;
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
        [InlineData(typeof(StoreCurrencyExchangeRate))]
        [InlineData(typeof(StoreNavigationMenu))]
        [InlineData(typeof(StoreNavigationMenuItem))]
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

        [Fact]
        public void StoreCurrencyExchangeRate_HasOneRatePerProviderPair()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreCurrencyExchangeRate));

            Assert.NotNull(modelEntity);

            var rateIndex = modelEntity!.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(
                    ["StoreId", "BaseCurrencyCode", "TargetCurrencyCode", "ProviderKey"]));
            var foreignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(rateIndex);
            Assert.True(rateIndex!.IsUnique);
            Assert.NotNull(foreignKey);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey!.DeleteBehavior);
        }

        [Fact]
        public void StoreNavigationMenu_HasOneActiveSystemNamePerStore()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreNavigationMenu));

            Assert.NotNull(modelEntity);

            var storeSystemNameIndex = modelEntity!.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "SystemName"]));
            var foreignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(storeSystemNameIndex);
            Assert.True(storeSystemNameIndex!.IsUnique);
            Assert.Contains("archived_at IS NULL", storeSystemNameIndex.GetFilter(), StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(foreignKey);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey!.DeleteBehavior);
        }

        [Fact]
        public void StoreNavigationMenuItem_UsesMenuAndParentRelationships()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreNavigationMenuItem));

            Assert.NotNull(modelEntity);

            var menuForeignKey = modelEntity!.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(StoreNavigationMenu)
                    && key.Properties.Any(property => property.Name == "MenuId"));
            var parentForeignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(StoreNavigationMenuItem)
                    && key.Properties.Any(property => property.Name == "ParentItemId"));
            var targetIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "TargetType", "TargetEntityPublicId"]));

            Assert.NotNull(menuForeignKey);
            Assert.Equal(DeleteBehavior.Cascade, menuForeignKey!.DeleteBehavior);
            Assert.NotNull(parentForeignKey);
            Assert.Equal(DeleteBehavior.Restrict, parentForeignKey!.DeleteBehavior);
            Assert.NotNull(targetIndex);
        }

        [Fact]
        public void VariationTemplateOption_MetadataFieldsHaveDefaults()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(VariationTemplateOption));

            Assert.NotNull(modelEntity);
            var controlType = modelEntity!.FindProperty(nameof(VariationTemplateOption.ControlType));
            var isRequired = modelEntity.FindProperty(nameof(VariationTemplateOption.IsRequired));

            Assert.NotNull(controlType);
            Assert.Equal("control_type", controlType!.GetColumnName());
            Assert.Equal(32, controlType.GetMaxLength());
            Assert.False(controlType.IsNullable);
            Assert.Equal(VariationControlTypes.Dropdown, controlType.GetDefaultValue());

            Assert.NotNull(isRequired);
            Assert.Equal("is_required", isRequired!.GetColumnName());
            Assert.False(isRequired.IsNullable);
            Assert.Equal(true, isRequired.GetDefaultValue());
        }

        [Fact]
        public void VariationTemplateValue_ColorHexHasMaxLength()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(VariationTemplateValue));

            Assert.NotNull(modelEntity);
            var colorHex = modelEntity!.FindProperty(nameof(VariationTemplateValue.ColorHex));

            Assert.NotNull(colorHex);
            Assert.Equal("color_hex", colorHex!.GetColumnName());
            Assert.Equal(7, colorHex.GetMaxLength());
            Assert.True(colorHex.IsNullable);
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
