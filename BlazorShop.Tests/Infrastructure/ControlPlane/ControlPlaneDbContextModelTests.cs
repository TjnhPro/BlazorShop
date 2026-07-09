namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    using Xunit;

    public class ControlPlaneDbContextModelTests
    {
        [Fact]
        public void ForeignKeyProperties_AreIndexed()
        {
            using var context = CreateContext();

            var missingIndexes = context.Model.GetEntityTypes()
                .SelectMany(
                    entityType => entityType.GetForeignKeys()
                        .SelectMany(
                            foreignKey => foreignKey.Properties.Select(
                                property => new
                                {
                                    EntityType = entityType,
                                    Property = property
                                })))
                .Where(candidate => !HasUsableIndex(candidate.EntityType, candidate.Property))
                .Select(candidate => $"{candidate.EntityType.GetTableName()}.{GetColumnName(candidate.EntityType, candidate.Property)}")
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(missingIndexes);
        }

        [Fact]
        public void CredentialEntity_DoesNotPersistRawSecretMaterial()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(CommerceNodeCredential));

            Assert.NotNull(entityType);

            var persistedNames = entityType!.GetProperties()
                .Select(property => new
                {
                    Property = property.Name,
                    Column = GetColumnName(entityType, property)
                })
                .ToArray();

            Assert.Contains(persistedNames, property => property.Property == nameof(CommerceNodeCredential.SecretHash) && property.Column == "secret_hash");

            var rawSecretNames = persistedNames
                .Where(property => IsRawSecretName(property.Property) || IsRawSecretName(property.Column))
                .Select(property => $"{property.Property}:{property.Column}")
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(rawSecretNames);
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev",
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(ControlPlaneDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure();
                    })
                .Options;

            return new ControlPlaneDbContext(options);
        }

        private static bool HasUsableIndex(IEntityType entityType, IProperty property)
        {
            return entityType.GetKeys().Any(key => StartsWithProperty(key.Properties, property))
                   || entityType.GetIndexes().Any(index => StartsWithProperty(index.Properties, property));
        }

        private static bool StartsWithProperty(IReadOnlyList<IProperty> properties, IProperty property)
        {
            return properties.Count > 0 && properties[0] == property;
        }

        private static string GetColumnName(IEntityType entityType, IProperty property)
        {
            var tableName = entityType.GetTableName();
            var storeObject = StoreObjectIdentifier.Table(tableName!, entityType.GetSchema());
            return property.GetColumnName(storeObject) ?? property.Name;
        }

        private static bool IsRawSecretName(string name)
        {
            var normalized = name.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

            return normalized is "secret" or "apikey" or "rawsecret" or "plainsecret" or "secretvalue" or "secrettext"
                   || normalized.Contains("rawapikey", StringComparison.Ordinal)
                   || normalized.Contains("plainapikey", StringComparison.Ordinal);
        }
    }
}
