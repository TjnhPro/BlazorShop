extern alias CommerceNodeApi;
extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Microsoft.AspNetCore.RateLimiting;
    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Configuration;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers;
    using StorefrontV2::BlazorShop.Storefront.Options;

    public sealed class SecurityPrivacyPhase2RateLimitTests
    {
        [Theory]
        [MemberData(nameof(StorefrontMutationPolicies))]
        public void CommerceNodeStorefrontMutations_HaveExpectedRateLimitPolicy(
            Type controllerType,
            string methodName,
            string expectedPolicy)
        {
            var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.NotNull(method);
            var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true);

            Assert.NotNull(attribute);
            Assert.Equal(expectedPolicy, attribute.PolicyName);
        }

        [Theory]
        [InlineData(typeof(StorefrontScopedCatalogController), nameof(StorefrontScopedCatalogController.GetProducts))]
        [InlineData(typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.Get))]
        [InlineData(typeof(StorefrontScopedPaymentsController), nameof(StorefrontScopedPaymentsController.GetPaymentMethods))]
        public void CommerceNodeStorefrontReadEndpoints_DoNotGetMutationRateLimitPolicy(Type controllerType, string methodName)
        {
            var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.NotNull(method);
            Assert.Null(method.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true));
        }

        [Fact]
        public void CommerceNodeProgram_ConfiguresStorefrontRateLimiterResponseAndPartitioning()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Program.cs");

            Assert.Contains("AddRateLimiter", program, StringComparison.Ordinal);
            Assert.Contains("UseRateLimiter", program, StringComparison.Ordinal);
            Assert.Contains("CommerceNodeApiErrorResponse", program, StringComparison.Ordinal);
            Assert.Contains("rate_limit_exceeded", program, StringComparison.Ordinal);
            Assert.Contains("MetadataName.RetryAfter", program, StringComparison.Ordinal);
            Assert.Contains("RouteValues.TryGetValue(\"storeKey\"", program, StringComparison.Ordinal);
            Assert.Contains("ClaimTypes.NameIdentifier", program, StringComparison.Ordinal);
            Assert.Contains("X-Robots-Tag", program, StringComparison.Ordinal);
            Assert.Contains("noindex, nofollow", program, StringComparison.Ordinal);
            Assert.Contains("Cache-Control", program, StringComparison.Ordinal);
            Assert.Contains("no-store, no-cache, max-age=0", program, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontLocalCartMutations_HaveRateLimitPolicyAndTypedResponse()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Options/StorefrontRateLimitingOptions.cs");

            Assert.Contains("Storefront:RateLimiting", options, StringComparison.Ordinal);
            Assert.Equal(4, Regex.Matches(program, "RequireRateLimiting\\(StorefrontLocalCartRateLimitPolicyName\\)").Count);
            Assert.Contains("StorefrontLocalCartErrorResponse(\"Too many cart requests. Try again shortly.\")", program, StringComparison.Ordinal);
            Assert.Contains("MetadataName.RetryAfter", program, StringComparison.Ordinal);
            Assert.Contains("StorefrontResponseHeaders.ApplyPrivatePage", program, StringComparison.Ordinal);
        }

        [Fact]
        public void RateLimitDefaults_ArePermissiveAndConfigurable()
        {
            var commerceNode = new CommerceNodeRateLimitingOptions();
            var storefront = new StorefrontRateLimitingOptions();

            Assert.True(commerceNode.Enabled);
            Assert.True(storefront.Enabled);
            Assert.True(commerceNode.Cart.PermitLimit >= 120);
            Assert.True(commerceNode.AuthStrict.PermitLimit >= 10);
            Assert.True(storefront.Cart.PermitLimit >= 120);
            Assert.Equal(0, commerceNode.Cart.QueueLimit);
            Assert.Equal(0, storefront.Cart.QueueLimit);
        }

        public static IEnumerable<object[]> StorefrontMutationPolicies()
        {
            yield return [typeof(StorefrontScopedAuthController), nameof(StorefrontScopedAuthController.Register), StorefrontRateLimitPolicyNames.AuthStrict];
            yield return [typeof(StorefrontScopedAuthController), nameof(StorefrontScopedAuthController.Login), StorefrontRateLimitPolicyNames.AuthStrict];
            yield return [typeof(StorefrontScopedAuthController), nameof(StorefrontScopedAuthController.RefreshToken), StorefrontRateLimitPolicyNames.AuthStrict];
            yield return [typeof(StorefrontScopedAuthController), nameof(StorefrontScopedAuthController.Logout), StorefrontRateLimitPolicyNames.AuthStrict];
            yield return [typeof(StorefrontScopedAuthController), nameof(StorefrontScopedAuthController.ChangePassword), StorefrontRateLimitPolicyNames.AuthStrict];
            yield return [typeof(StorefrontScopedAuthController), nameof(StorefrontScopedAuthController.UpdateProfile), StorefrontRateLimitPolicyNames.AuthStrict];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.CreateSession), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.AddLine), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.UpdateLine), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.RemoveLine), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.Clear), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.Validate), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCartController), nameof(StorefrontScopedCartController.SaveCheckout), StorefrontRateLimitPolicyNames.Cart];
            yield return [typeof(StorefrontScopedCurrencyController), nameof(StorefrontScopedCurrencyController.SetPreference), StorefrontRateLimitPolicyNames.Currency];
            yield return [typeof(StorefrontScopedCheckoutController), nameof(StorefrontScopedCheckoutController.Preview), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedCheckoutController), nameof(StorefrontScopedCheckoutController.PlaceOrder), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedNewsletterController), nameof(StorefrontScopedNewsletterController.Subscribe), StorefrontRateLimitPolicyNames.Newsletter];
            yield return [typeof(StorefrontScopedOrdersController), nameof(StorefrontScopedOrdersController.ConfirmOrder), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedPaymentsController), nameof(StorefrontScopedPaymentsController.HandleProviderCallback), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedPaymentsController), nameof(StorefrontScopedPaymentsController.HandleWebhook), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedPaymentsController), nameof(StorefrontScopedPaymentsController.CapturePayPal), StorefrontRateLimitPolicyNames.Checkout];
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
