extern alias CommerceNodeApi;
extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
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
            var dbContext = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDbContext.cs");
            var migration = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Migrations");

            Assert.Contains("DbSet<StorefrontConsentState>", dbContext, StringComparison.Ordinal);
            Assert.Contains("DbSet<StorefrontConsentEvent>", dbContext, StringComparison.Ordinal);
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
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("<StorefrontConsentBanner />", layout, StringComparison.Ordinal);
            Assert.Contains("data-storefront-consent-banner", banner, StringComparison.Ordinal);
            Assert.Contains("data-storefront-consent-revoke", banner, StringComparison.Ordinal);
            Assert.Contains("/api/consent/current", program, StringComparison.Ordinal);
            Assert.Contains("/api/consent/revoke", program, StringComparison.Ordinal);
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
    }
}
