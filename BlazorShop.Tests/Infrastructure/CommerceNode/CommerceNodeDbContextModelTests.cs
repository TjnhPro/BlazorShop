namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
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
        [InlineData(typeof(StoreShippingSettings))]
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
        public void StoreShippingSettings_HasOneSettingsRowPerStore()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(StoreShippingSettings));

            Assert.NotNull(modelEntity);
            Assert.Equal("store_shipping_settings", modelEntity!.GetTableName());

            var storeIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId"]));
            var publicIdIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["PublicId"]));
            var foreignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));

            Assert.NotNull(storeIndex);
            Assert.True(storeIndex!.IsUnique);
            Assert.NotNull(publicIdIndex);
            Assert.True(publicIdIndex!.IsUnique);
            Assert.NotNull(foreignKey);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey!.DeleteBehavior);
            Assert.Equal("sum", modelEntity.FindProperty(nameof(StoreShippingSettings.SurchargePolicy))!.GetDefaultValue());
            Assert.Equal(2, modelEntity.FindProperty(nameof(StoreShippingSettings.OriginCountryCode))!.GetMaxLength());
            Assert.Equal(16, modelEntity.FindProperty(nameof(StoreShippingSettings.SurchargePolicy))!.GetMaxLength());
            Assert.Equal(128, modelEntity.FindProperty(nameof(StoreShippingSettings.DefaultDeliveryEstimateText))!.GetMaxLength());
        }

        [Fact]
        public void CommerceCustomerAddress_HasStoreCustomerScopeAndDefaultIndexes()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(CommerceCustomerAddress));

            Assert.NotNull(modelEntity);
            Assert.Equal("commerce_customer_addresses", modelEntity!.GetTableName());

            var publicIdIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["PublicId"]));
            var lookupIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "CustomerId", "DeletedAtUtc"]));
            var countryIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "CustomerId", "CountryCode"]));
            var defaultShippingIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "CustomerId", "IsDefaultShipping"]));
            var defaultBillingIndex = modelEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["StoreId", "CustomerId", "IsDefaultBilling"]));
            var storeForeignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceStore)
                    && key.Properties.Any(property => property.Name == "StoreId"));
            var customerForeignKey = modelEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(CommerceCustomer)
                    && key.Properties.Any(property => property.Name == "CustomerId"));

            Assert.NotNull(publicIdIndex);
            Assert.True(publicIdIndex!.IsUnique);
            Assert.NotNull(lookupIndex);
            Assert.NotNull(countryIndex);
            Assert.NotNull(defaultShippingIndex);
            Assert.True(defaultShippingIndex!.IsUnique);
            Assert.Contains("is_default_shipping = true", defaultShippingIndex.GetFilter(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("deleted_at_utc IS NULL", defaultShippingIndex.GetFilter(), StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(defaultBillingIndex);
            Assert.True(defaultBillingIndex!.IsUnique);
            Assert.Contains("is_default_billing = true", defaultBillingIndex.GetFilter(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("deleted_at_utc IS NULL", defaultBillingIndex.GetFilter(), StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(storeForeignKey);
            Assert.Equal(DeleteBehavior.Cascade, storeForeignKey!.DeleteBehavior);
            Assert.NotNull(customerForeignKey);
            Assert.Equal(DeleteBehavior.Cascade, customerForeignKey!.DeleteBehavior);
            Assert.Equal(2, modelEntity.FindProperty(nameof(CommerceCustomerAddress.CountryCode))!.GetMaxLength());
            Assert.Equal(240, modelEntity.FindProperty(nameof(CommerceCustomerAddress.Address1))!.GetMaxLength());
            Assert.Equal(false, modelEntity.FindProperty(nameof(CommerceCustomerAddress.IsDefaultShipping))!.GetDefaultValue());
            Assert.Equal(false, modelEntity.FindProperty(nameof(CommerceCustomerAddress.IsDefaultBilling))!.GetDefaultValue());
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

        [Fact]
        public void ProductVariant_IsActiveDefaultsToTrue()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(ProductVariant));

            Assert.NotNull(modelEntity);
            var isActive = modelEntity!.FindProperty(nameof(ProductVariant.IsActive));

            Assert.NotNull(isActive);
            Assert.False(isActive!.IsNullable);
            Assert.Equal(true, isActive.GetDefaultValue());
        }

        [Fact]
        public void ProductPurchaseFields_HaveSafeDefaultsAndMaxLengths()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(Product));

            Assert.NotNull(modelEntity);

            Assert.Equal(1, modelEntity!.FindProperty(nameof(Product.MinOrderQuantity))!.GetDefaultValue());
            Assert.Equal(1, modelEntity.FindProperty(nameof(Product.QuantityStep))!.GetDefaultValue());
            Assert.Equal(false, modelEntity.FindProperty(nameof(Product.PurchasingDisabled))!.GetDefaultValue());
            Assert.Equal(true, modelEntity.FindProperty(nameof(Product.ManageStock))!.GetDefaultValue());
            Assert.Equal(false, modelEntity.FindProperty(nameof(Product.HideWhenOutOfStock))!.GetDefaultValue());
            Assert.Equal(true, modelEntity.FindProperty(nameof(Product.ShippingRequired))!.GetDefaultValue());
            Assert.Equal(false, modelEntity.FindProperty(nameof(Product.FreeShipping))!.GetDefaultValue());
            Assert.Equal(18, modelEntity.FindProperty(nameof(Product.ShippingSurcharge))!.GetPrecision());
            Assert.Equal(2, modelEntity.FindProperty(nameof(Product.ShippingSurcharge))!.GetScale());
            Assert.Equal(
                ProductPurchaseConstraints.PurchasingDisabledReasonMaxLength,
                modelEntity.FindProperty(nameof(Product.PurchasingDisabledReason))!.GetMaxLength());
            Assert.Equal(
                ProductPurchaseConstraints.DeliveryEstimateTextMaxLength,
                modelEntity.FindProperty(nameof(Product.DeliveryEstimateText))!.GetMaxLength());
        }

        [Fact]
        public void OrderShippingSnapshotFields_HaveSafeLengthsAndPrecision()
        {
            using var context = CreateContext();
            var modelEntity = context.Model.FindEntityType(typeof(Order));

            Assert.NotNull(modelEntity);
            Assert.Equal(64, modelEntity!.FindProperty(nameof(Order.ShippingMethodKey))!.GetMaxLength());
            Assert.Equal(64, modelEntity.FindProperty(nameof(Order.ShippingProviderSystemName))!.GetMaxLength());
            Assert.Equal(64, modelEntity.FindProperty(nameof(Order.ShippingMethodCode))!.GetMaxLength());
            Assert.Equal(128, modelEntity.FindProperty(nameof(Order.ShippingMethodName))!.GetMaxLength());
            Assert.Equal(18, modelEntity.FindProperty(nameof(Order.ShippingTotal))!.GetPrecision());
            Assert.Equal(2, modelEntity.FindProperty(nameof(Order.ShippingTotal))!.GetScale());
            Assert.Equal(3, modelEntity.FindProperty(nameof(Order.ShippingCurrencyCode))!.GetMaxLength());
            Assert.Equal(128, modelEntity.FindProperty(nameof(Order.ShippingDeliveryEstimateText))!.GetMaxLength());
        }

        [Fact]
        public void ShipmentItemsAndTrackingEvents_HaveSafeRelationshipsAndLengths()
        {
            using var context = CreateContext();
            var shipmentItemEntity = context.Model.FindEntityType(typeof(ShipmentItem));
            var trackingEventEntity = context.Model.FindEntityType(typeof(ShipmentTrackingEvent));

            Assert.NotNull(shipmentItemEntity);
            Assert.NotNull(trackingEventEntity);
            Assert.Equal("ShipmentItems", shipmentItemEntity!.GetTableName());
            Assert.Equal("ShipmentTrackingEvents", trackingEventEntity!.GetTableName());

            var shipmentLineIndex = shipmentItemEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(["ShipmentId", "OrderLineId"]));
            var orderLineForeignKey = shipmentItemEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(OrderLine));
            var shipmentItemForeignKey = shipmentItemEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Shipment));
            var shipmentEventForeignKey = trackingEventEntity.GetForeignKeys()
                .SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Shipment));

            Assert.NotNull(shipmentLineIndex);
            Assert.True(shipmentLineIndex!.IsUnique);
            Assert.NotNull(orderLineForeignKey);
            Assert.Equal(DeleteBehavior.Restrict, orderLineForeignKey!.DeleteBehavior);
            Assert.NotNull(shipmentItemForeignKey);
            Assert.Equal(DeleteBehavior.Cascade, shipmentItemForeignKey!.DeleteBehavior);
            Assert.NotNull(shipmentEventForeignKey);
            Assert.Equal(DeleteBehavior.Cascade, shipmentEventForeignKey!.DeleteBehavior);
            Assert.Equal(64, trackingEventEntity.FindProperty(nameof(ShipmentTrackingEvent.Status))!.GetMaxLength());
            Assert.Equal(500, trackingEventEntity.FindProperty(nameof(ShipmentTrackingEvent.Message))!.GetMaxLength());
            Assert.Equal(160, trackingEventEntity.FindProperty(nameof(ShipmentTrackingEvent.Location))!.GetMaxLength());
            Assert.Equal(64, trackingEventEntity.FindProperty(nameof(ShipmentTrackingEvent.Source))!.GetMaxLength());
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
