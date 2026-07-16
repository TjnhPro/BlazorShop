extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Contracts.Storefront;

    public sealed class SecurityPrivacyPhase4CaptchaTests
    {
        [Fact]
        public void CaptchaContracts_ExposePublicMetadataWithoutSecret()
        {
            var configType = typeof(StorefrontCaptchaConfigurationResponse);
            var propertyNames = string.Join('|', configType.GetProperties().Select(property => property.Name));

            Assert.Contains("ProviderSystemName", propertyNames, StringComparison.Ordinal);
            Assert.Contains("PublicSiteKey", propertyNames, StringComparison.Ordinal);
            Assert.Contains("EnabledTargets", propertyNames, StringComparison.Ordinal);
            Assert.DoesNotContain("Secret", propertyNames, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Private", propertyNames, StringComparison.OrdinalIgnoreCase);

            var options = ReadRepositoryFile("BlazorShop.Application/CommerceNode/Captcha/CaptchaOptions.cs");
            Assert.Contains("SecretReference", options, StringComparison.Ordinal);
        }

        [Fact]
        public void CaptchaTargetRequests_HaveOptionalToken()
        {
            Assert.NotNull(typeof(StorefrontLoginRequest).GetProperty("CaptchaToken"));
            Assert.NotNull(typeof(StorefrontRegisterRequest).GetProperty("CaptchaToken"));
            Assert.NotNull(typeof(StorefrontNewsletterSubscribeRequest).GetProperty("CaptchaToken"));
        }

        [Fact]
        public void CaptchaServerSideVerifier_IsProviderNeutralAndDisabledByDefault()
        {
            var options = ReadRepositoryFile("BlazorShop.Application/CommerceNode/Captcha/CaptchaOptions.cs");
            var verifier = ReadRepositoryFile("BlazorShop.Application/CommerceNode/Captcha/ICaptchaVerifier.cs");
            var noop = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/NoopCaptchaVerifier.cs");
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");

            Assert.Contains("public bool Enabled { get; set; }", options, StringComparison.Ordinal);
            Assert.Contains("ICaptchaVerifier", verifier, StringComparison.Ordinal);
            Assert.Contains("CaptchaVerificationRequest", verifier, StringComparison.Ordinal);
            Assert.Contains("CaptchaVerificationResult.Passed", noop, StringComparison.Ordinal);
            Assert.Contains("ValidateCaptchaAsync", controller, StringComparison.Ordinal);
            Assert.Contains("securityPrivacySettingsService.ResolveCurrentAsync", controller, StringComparison.Ordinal);
            Assert.Contains("!IsCaptchaEnabled(runtimeSettings.Captcha, target)", controller, StringComparison.Ordinal);
            Assert.Contains("return result.Success", controller, StringComparison.Ordinal);
            Assert.Contains("captcha.required", controller, StringComparison.Ordinal);
            Assert.Contains("captcha.failed", controller, StringComparison.Ordinal);
        }

        [Fact]
        public void CaptchaTargets_AreExplicitAndDoNotEnableCheckoutByDefault()
        {
            var targetNames = ReadRepositoryFile("BlazorShop.Application/CommerceNode/Captcha/CaptchaTargetNames.cs");
            var options = ReadRepositoryFile("BlazorShop.Application/CommerceNode/Captcha/CaptchaOptions.cs");

            Assert.Contains("Login", targetNames, StringComparison.Ordinal);
            Assert.Contains("Registration", targetNames, StringComparison.Ordinal);
            Assert.Contains("Newsletter", targetNames, StringComparison.Ordinal);
            Assert.Contains("PasswordRecovery", targetNames, StringComparison.Ordinal);
            Assert.Contains("Contact", targetNames, StringComparison.Ordinal);
            Assert.Contains("Review", targetNames, StringComparison.Ordinal);
            Assert.DoesNotContain("Checkout", targetNames, StringComparison.Ordinal);
            Assert.DoesNotContain("Checkout", options, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontForms_RenderCaptchaTokenHooks()
        {
            var signIn = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/SignInPage.razor");
            var register = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/RegisterPage.razor");

            Assert.Contains("data-storefront-captcha-token=\"login\"", signIn, StringComparison.Ordinal);
            Assert.Contains("data-storefront-captcha-token=\"registration\"", register, StringComparison.Ordinal);
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
