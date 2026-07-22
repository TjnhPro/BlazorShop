extern alias CommerceNodeApi;
extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Contracts.Storefront;

    public sealed class SecurityPrivacyPhase3ConsentTests
    {
        [Fact]
        public void ConsentDomain_DoesNotStoreRawVisitorKey()
        {
            var entity = ReadRepositoryFile("BlazorShop.Domain/Entities/CommerceNode/StorefrontConsentState.cs");
            var service = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontConsentService.cs");

            Assert.Contains("VisitorKeyHash", entity, StringComparison.Ordinal);
            Assert.DoesNotContain("VisitorKey { get; set; }", entity, StringComparison.Ordinal);
            Assert.Contains("SHA256.HashData", service, StringComparison.Ordinal);
            Assert.Contains("HashVisitorKey(visitorKey)", service, StringComparison.Ordinal);
        }

        [Fact]
        public void ConsentCore_IsStoreScopedAndMigrated()
        {
            using var context = CreateContext();
            var contextProperties = typeof(CommerceNodeDbContext)
                .GetProperties()
                .Select(property => property.PropertyType)
                .ToArray();
            var stateEntity = context.Model.FindEntityType(typeof(StorefrontConsentState));
            var eventEntity = context.Model.FindEntityType(typeof(StorefrontConsentEvent));
            var migration = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Migrations");

            Assert.Contains(typeof(DbSet<StorefrontConsentState>), contextProperties);
            Assert.Contains(typeof(DbSet<StorefrontConsentEvent>), contextProperties);
            Assert.NotNull(stateEntity);
            Assert.NotNull(eventEntity);
            Assert.Equal("storefront_consent_state", stateEntity!.GetTableName());
            Assert.Equal("storefront_consent_event", eventEntity!.GetTableName());
            Assert.NotNull(stateEntity.FindProperty(nameof(StorefrontConsentState.StoreId)));
            Assert.NotNull(eventEntity.FindProperty(nameof(StorefrontConsentEvent.StoreId)));
            Assert.NotNull(stateEntity.FindProperty(nameof(StorefrontConsentState.VisitorKeyHash)));
            Assert.Contains("storefront_consent_state", migration, StringComparison.Ordinal);
            Assert.Contains("storefront_consent_event", migration, StringComparison.Ordinal);
            Assert.Contains("store_id", migration, StringComparison.Ordinal);
            Assert.Contains("visitor_key_hash", migration, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontPublicConfiguration_ExposesOnlySafeConsentMetadata()
        {
            var responseType = typeof(StorefrontPublicConfigurationResponse);
            var consentType = typeof(StorefrontConsentConfigurationResponse);
            var propertyNames = string.Join(
                '|',
                responseType.GetProperties().Select(property => property.Name)
                    .Concat(consentType.GetProperties().Select(property => property.Name)));

            Assert.Contains("Consent", propertyNames, StringComparison.Ordinal);
            Assert.Contains("CurrentVersion", propertyNames, StringComparison.Ordinal);
            Assert.Contains("PolicyPagePath", propertyNames, StringComparison.Ordinal);
            Assert.DoesNotContain("VisitorKey", propertyNames, StringComparison.Ordinal);
            Assert.DoesNotContain("Secret", propertyNames, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StorefrontV2_RendersConsentBannerAndUsesLocalProxyEndpoints()
        {
            var layout = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");
            var banner = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Security/StorefrontConsentBanner.razor");
            var consentEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontConsentEndpoints.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("<StorefrontConsentBanner />", layout, StringComparison.Ordinal);
            Assert.Contains("data-storefront-consent-banner", banner, StringComparison.Ordinal);
            Assert.Contains("pointer-events-auto", banner, StringComparison.Ordinal);
            Assert.Contains("z-[100]", banner, StringComparison.Ordinal);
            Assert.Contains("data-storefront-consent-revoke", banner, StringComparison.Ordinal);
            Assert.Contains("/api/consent/current", consentEndpoints, StringComparison.Ordinal);
            Assert.Contains("/api/consent/revoke", consentEndpoints, StringComparison.Ordinal);
            Assert.Contains("data-storefront-consent-manage", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontFooter.razor"), StringComparison.Ordinal);
            Assert.Contains("sendConsentRequest", script, StringComparison.Ordinal);
            Assert.Contains("readAntiforgeryHeader", script, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, relativePath)))
                {
                    return string.Join(
                        Environment.NewLine,
                        Directory.GetFiles(Path.Combine(directory.FullName, relativePath), "*StorefrontConsentCore*", SearchOption.AllDirectories)
                            .Select(File.ReadAllText));
                }

                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
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
