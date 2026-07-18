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
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Controllers/CommerceGateway/ControlPlaneCommerceSecurityPrivacyController.cs");
            var gateway = ReadRepositoryFile("BlazorShop.Infrastructure/Data/ControlPlane/CommerceGateway/ControlPlaneSecurityPrivacyGateway.cs");
            var webClient = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Commerce/SecurityPrivacy/ControlPlaneSecurityPrivacyClient.cs");
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceSecurityPrivacy.razor");
            var nav = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Layout/NavMenu.razor");

            Assert.Contains("ControlPlanePolicyNames.CommerceSecurityPrivacyRead", controller, StringComparison.Ordinal);
            Assert.Contains("ControlPlanePolicyNames.CommerceSecurityPrivacyWrite", controller, StringComparison.Ordinal);
            Assert.Contains("api/commerce/admin/security-privacy", gateway, StringComparison.Ordinal);
            Assert.Contains("GetSecurityPrivacySettingsAsync", webClient, StringComparison.Ordinal);
            Assert.Contains("UpdateSecurityPrivacySettingsAsync", webClient, StringComparison.Ordinal);
            Assert.Contains("commerce-admin/security-privacy", page, StringComparison.Ordinal);
            Assert.Contains("Security/Privacy", nav, StringComparison.Ordinal);
            Assert.DoesNotContain("api/commerce", page, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("X-Node-Key", page, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("X-Node-Secret", page, StringComparison.OrdinalIgnoreCase);
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
        public void SecurityPrivacyContracts_ExposeStoreScopedRegistrationPolicy()
        {
            var responseType = typeof(StoreSecurityPrivacySettingsDto);
            var requestType = typeof(UpdateStoreSecurityPrivacySettingsRequest);
            var runtimeType = typeof(StoreSecurityPrivacyRuntimeSettings);

            Assert.NotNull(responseType.GetProperty("Registration"));
            Assert.NotNull(requestType.GetProperty("Registration"));
            Assert.NotNull(runtimeType.GetProperty("Registration"));
            Assert.NotNull(typeof(StoreSecurityPrivacySettings).GetProperty("RegistrationMode"));
            Assert.Equal("standard", new StoreSecurityPrivacySettings().RegistrationMode);
        }

        [Fact]
        public void StorefrontRuntime_ResolvesCaptchaConsentAndRegistrationFromSecurityPrivacyService()
        {
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");
            var service = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/StoreSecurityPrivacySettingsService.cs");

            Assert.Contains("IStoreSecurityPrivacySettingsService", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettingsService.ResolveCurrentAsync", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettings.Consent", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettings.Captcha", controller, StringComparison.Ordinal);
            Assert.Contains("runtimeSettings.Registration", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("runtimeOptions.Security.RegistrationMode", controller, StringComparison.Ordinal);
            Assert.Contains("Registration mode must be either standard or disabled.", service, StringComparison.Ordinal);
            Assert.Contains("RegistrationMode", service, StringComparison.Ordinal);
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
