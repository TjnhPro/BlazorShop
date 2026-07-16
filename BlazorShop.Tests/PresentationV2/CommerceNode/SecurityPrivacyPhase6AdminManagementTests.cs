namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Xunit;

    public sealed class SecurityPrivacyPhase6AdminManagementTests
    {
        [Fact]
        public void SecurityPrivacyPermissions_AreGranularAndSeeded()
        {
            Assert.Contains(ControlPlanePermissions.CommerceSecurityPrivacyRead, ControlPlanePermissions.All);
            Assert.Contains(ControlPlanePermissions.CommerceSecurityPrivacyWrite, ControlPlanePermissions.All);
            Assert.Contains(ControlPlanePermissions.CommerceCaptchaSettingsEdit, ControlPlanePermissions.All);
            Assert.Contains(ControlPlanePermissions.CommerceConsentSettingsEdit, ControlPlanePermissions.All);

            var dbContext = ReadRepositoryFile("BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneDbContext.cs");
            Assert.Contains("commerce.security_privacy.read", dbContext, StringComparison.Ordinal);
            Assert.Contains("commerce.security_privacy.write", dbContext, StringComparison.Ordinal);
            Assert.Contains("commerce.captcha_settings.edit", dbContext, StringComparison.Ordinal);
            Assert.Contains("commerce.consent_settings.edit", dbContext, StringComparison.Ordinal);
        }

        [Fact]
        public void ControlPlaneGateway_UsesSecurityPrivacyPoliciesAndDoesNotExposeCommerceNodeToWeb()
        {
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Controllers/ControlPlaneCommerceCatalogController.cs");
            var gateway = ReadRepositoryFile("BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneCommerceCatalogService.cs");

            Assert.Contains("ControlPlanePolicyNames.CommerceSecurityPrivacyRead", controller, StringComparison.Ordinal);
            Assert.Contains("ControlPlanePolicyNames.CommerceSecurityPrivacyWrite", controller, StringComparison.Ordinal);
            Assert.Contains("api/commerce/admin/security-privacy", gateway, StringComparison.Ordinal);
        }

        [Fact]
        public void SecurityPrivacyContracts_MaskCaptchaSecretState()
        {
            var responseType = typeof(StoreCaptchaAdminSettingsDto);
            var propertyNames = string.Join('|', responseType.GetProperties().Select(property => property.Name));

            Assert.Contains("SecretConfigured", propertyNames, StringComparison.Ordinal);
            Assert.Contains("LastRotatedAt", propertyNames, StringComparison.Ordinal);
            Assert.Contains("ProviderDisplayName", propertyNames, StringComparison.Ordinal);
            Assert.DoesNotContain("SecretReference", propertyNames, StringComparison.Ordinal);
            Assert.DoesNotContain("SecretValue", propertyNames, StringComparison.Ordinal);

            Assert.NotNull(typeof(StoreSecurityPrivacySettings).GetProperty("CaptchaSecretReference"));
        }

        [Fact]
        public void StorefrontRuntime_ResolvesCaptchaAndConsentFromSecurityPrivacyService()
        {
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");

            Assert.Contains("IStoreSecurityPrivacySettingsService", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettingsService.ResolveCurrentAsync", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettings.Consent", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettings.Captcha", controller, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
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
