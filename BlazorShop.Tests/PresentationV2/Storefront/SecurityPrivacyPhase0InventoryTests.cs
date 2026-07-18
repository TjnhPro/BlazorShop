extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using StorefrontV2::BlazorShop.Storefront.Services;
    using Xunit;

    public sealed class SecurityPrivacyPhase0InventoryTests
    {
        [Fact]
        public void StorefrontV2_BrowserMutationRoutesMatchSecurityPrivacyInventory()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var pipeline = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontApplicationBuilderExtensions.cs");

            Assert.Contains("app.UseStorefrontV2HostPipeline(storefrontRateLimitingOptions);", program, StringComparison.Ordinal);
            Assert.Contains("app.UseAntiforgery();", pipeline, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(StorefrontRoutes.SignIn", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(StorefrontRoutes.Register", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(StorefrontRoutes.Logout", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(StorefrontRoutes.CurrencyPreference", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(StorefrontRoutes.Checkout", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/cart/lines\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPut(\"/api/cart/lines/{lineId:guid}\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapDelete(\"/api/cart/lines/{lineId:guid}\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapDelete(\"/api/cart\"", program, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNode_StorefrontMutationRoutesMatchSecurityPrivacyInventory()
        {
            var controllers = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/auth\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"register\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"login\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"refresh-token\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"logout\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"change-password\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"update-profile\")]", controllers, StringComparison.Ordinal);

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/cart\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"session\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"lines\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"validate\")]", controllers, StringComparison.Ordinal);

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/checkout\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"preview\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"place-order\")]", controllers, StringComparison.Ordinal);

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/currency\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"preference\")]", controllers, StringComparison.Ordinal);

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/newsletter\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpPost(\"subscribe\")]", controllers, StringComparison.Ordinal);

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/orders\")]", controllers, StringComparison.Ordinal);
            Assert.DoesNotContain("[HttpPost(\"confirm\")]", controllers, StringComparison.Ordinal);
        }

        [Fact]
        public void CookieInventory_HasNamedEssentialAndPreferenceCookies()
        {
            var authCookies = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontAuthCookies.cs");
            var commerceRuntimeOptions = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Configuration/CommerceNodeRuntimeOptions.cs");
            var cartTokenService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontCartTokenService.cs");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var pipeline = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontApplicationBuilderExtensions.cs");
            var cookieNames = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/StorefrontCookieNames.cs");

            Assert.Contains("Cart = \"my-cart\"", cookieNames, StringComparison.Ordinal);
            Assert.Contains("CartToken = \"bs-cart-token\"", cookieNames, StringComparison.Ordinal);
            Assert.Contains("CurrencyPreference = \"bs-currency\"", cookieNames, StringComparison.Ordinal);
            Assert.Contains("__Host-blazorshop-refresh", authCookies, StringComparison.Ordinal);
            Assert.Contains("__Host-blazorshop-refresh", commerceRuntimeOptions, StringComparison.Ordinal);
            Assert.Contains("IsEssential = true", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs"), StringComparison.Ordinal);
            Assert.Contains("StorefrontCookieNames.Cart", cartTokenService, StringComparison.Ordinal);
            Assert.Contains("StorefrontCookieNames.CartToken", cartTokenService, StringComparison.Ordinal);
            Assert.Contains("StorefrontCookieNames.CurrencyPreference", program, StringComparison.Ordinal);
            Assert.Contains("UseAntiforgery", pipeline, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("https://evil.example/", "/")]
        [InlineData("//evil.example/", "/")]
        [InlineData("/\\evil", "/")]
        [InlineData("/checkout\r\nSet-Cookie:bad=1", "/")]
        [InlineData("/checkout", "/checkout")]
        public void StorefrontReturnUrl_Normalize_RejectsUnsafeReturnUrls(string returnUrl, string expected)
        {
            Assert.Equal(expected, StorefrontReturnUrl.Normalize(returnUrl));
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
