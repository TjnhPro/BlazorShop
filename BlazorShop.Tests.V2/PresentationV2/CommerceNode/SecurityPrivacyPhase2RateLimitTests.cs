extern alias CommerceNodeApi;
extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Security.Claims;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using BlazorShop.Web.SharedV2;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.RateLimiting;
    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Configuration;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers;
    using CommerceNodeRateLimitIdentity = CommerceNodeApi::BlazorShop.CommerceNode.API.Configuration.StorefrontRateLimitIdentity;
    using StorefrontRateLimitIdentity = StorefrontV2::BlazorShop.Storefront.Configuration.StorefrontRateLimitIdentity;
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
            var identity = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Configuration/StorefrontRateLimitIdentity.cs");

            Assert.Contains("AddRateLimiter", program, StringComparison.Ordinal);
            Assert.Contains("UseRateLimiter", program, StringComparison.Ordinal);
            Assert.Contains("CommerceNodeApiErrorResponse", program, StringComparison.Ordinal);
            Assert.Contains("rate_limit_exceeded", program, StringComparison.Ordinal);
            Assert.Contains("MetadataName.RetryAfter", program, StringComparison.Ordinal);
            Assert.Contains("RouteValues.TryGetValue(\"storeKey\"", program, StringComparison.Ordinal);
            Assert.Contains("StorefrontRateLimitIdentity.ResolveActor(httpContext)", program, StringComparison.Ordinal);
            Assert.Contains("ClaimTypes.NameIdentifier", identity, StringComparison.Ordinal);
            Assert.Contains("CartTokenHeaderName = \"X-Cart-Token\"", identity, StringComparison.Ordinal);
            Assert.Contains("X-Robots-Tag", program, StringComparison.Ordinal);
            Assert.Contains("noindex, nofollow", program, StringComparison.Ordinal);
            Assert.Contains("Cache-Control", program, StringComparison.Ordinal);
            Assert.Contains("no-store, no-cache, max-age=0", program, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontLocalCartMutations_HaveRateLimitPolicyAndTypedResponse()
        {
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCartEndpoints.cs");
            var support = ReadStorefrontLocalEndpointSupportSource();
            var pipeline = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontApplicationBuilderExtensions.cs");
            var ratePolicies = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontRateLimitPolicies.cs");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Options/StorefrontRateLimitingOptions.cs");

            Assert.Contains("Storefront:RateLimiting", options, StringComparison.Ordinal);
            Assert.Contains("UseRateLimiter", pipeline, StringComparison.Ordinal);
            Assert.Equal(4, Regex.Matches(cartEndpoints, "RequireRateLimiting\\(StorefrontRateLimitPolicies\\.LocalCartPolicyName\\)").Count);
            Assert.Contains("LocalCartPolicyName = \"storefront-local-cart\"", ratePolicies, StringComparison.Ordinal);
            Assert.Contains("StorefrontRateLimitIdentity.ResolveLocalCartActor(httpContext)", ratePolicies, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalCartErrorResponse(\"Too many cart requests. Try again shortly.\")", ratePolicies, StringComparison.Ordinal);
            Assert.Contains("MetadataName.RetryAfter", ratePolicies, StringComparison.Ordinal);
            Assert.Contains("StorefrontResponseHeaders.ApplyPrivatePage", support, StringComparison.Ordinal);
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

        [Fact]
        public void CommerceNodeRateLimitIdentity_AuthenticatedUserOverridesCartToken()
        {
            var context = CreateContext("/api/storefront/stores/default/cart/lines", "203.0.113.10");
            context.Request.Headers[CommerceNodeRateLimitIdentity.CartTokenHeaderName] = "cart-token-a";
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "customer-1")],
                authenticationType: "Bearer"));

            var actor = CommerceNodeRateLimitIdentity.ResolveActor(context);

            Assert.Equal("user:customer-1", actor);
        }

        [Fact]
        public void CommerceNodeRateLimitIdentity_UsesHashedCartTokenForGuestCartAndCheckout()
        {
            var first = CreateContext("/api/storefront/stores/default/cart/lines", "203.0.113.10");
            var second = CreateContext("/api/storefront/stores/default/checkout/start", "203.0.113.10");
            first.Request.Headers[CommerceNodeRateLimitIdentity.CartTokenHeaderName] = "cart-token-a";
            second.Request.Headers[CommerceNodeRateLimitIdentity.CartTokenHeaderName] = "cart-token-b";

            var firstActor = CommerceNodeRateLimitIdentity.ResolveActor(first);
            var secondActor = CommerceNodeRateLimitIdentity.ResolveActor(second);

            Assert.StartsWith("cart:", firstActor, StringComparison.Ordinal);
            Assert.StartsWith("cart:", secondActor, StringComparison.Ordinal);
            Assert.NotEqual(firstActor, secondActor);
            Assert.DoesNotContain("cart-token-a", firstActor, StringComparison.Ordinal);
            Assert.DoesNotContain("cart-token-b", secondActor, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeRateLimitIdentity_UsesSameBucketForSameCartSession()
        {
            var first = CreateContext("/api/storefront/stores/default/cart/lines", "203.0.113.10");
            var second = CreateContext("/api/storefront/stores/default/cart/lines", "203.0.113.11");
            first.Request.Headers[CommerceNodeRateLimitIdentity.CartTokenHeaderName] = "cart-token-a";
            second.Request.Headers[CommerceNodeRateLimitIdentity.CartTokenHeaderName] = "cart-token-a";

            Assert.Equal(
                CommerceNodeRateLimitIdentity.ResolveActor(first),
                CommerceNodeRateLimitIdentity.ResolveActor(second));
        }

        [Fact]
        public void CommerceNodeRateLimitIdentity_IgnoresRawForwardedForWhenNoTrustedMiddlewareChangedRemoteIp()
        {
            var first = CreateContext("/api/storefront/stores/default/newsletter", "203.0.113.10");
            var second = CreateContext("/api/storefront/stores/default/newsletter", "203.0.113.10");
            first.Request.Headers["X-Forwarded-For"] = "198.51.100.10";
            second.Request.Headers["X-Forwarded-For"] = "198.51.100.11";

            Assert.Equal(
                CommerceNodeRateLimitIdentity.ResolveActor(first),
                CommerceNodeRateLimitIdentity.ResolveActor(second));
        }

        [Fact]
        public void StorefrontLocalCartRateLimitIdentity_UsesHashedCartCookieBeforeIp()
        {
            var first = CreateContext("/api/cart/lines", "203.0.113.10");
            var second = CreateContext("/api/cart/lines", "203.0.113.10");
            first.Request.Headers.Cookie = $"{StorefrontCookieNames.CartToken}=local-cart-a";
            second.Request.Headers.Cookie = $"{StorefrontCookieNames.CartToken}=local-cart-b";

            var firstActor = StorefrontRateLimitIdentity.ResolveLocalCartActor(first);
            var secondActor = StorefrontRateLimitIdentity.ResolveLocalCartActor(second);

            Assert.StartsWith("cart:", firstActor, StringComparison.Ordinal);
            Assert.StartsWith("cart:", secondActor, StringComparison.Ordinal);
            Assert.NotEqual(firstActor, secondActor);
            Assert.DoesNotContain("local-cart-a", firstActor, StringComparison.Ordinal);
            Assert.DoesNotContain("local-cart-b", secondActor, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontLocalCartRateLimitIdentity_UsesSameBucketForSameCartCookie()
        {
            var first = CreateContext("/api/cart/lines", "203.0.113.10");
            var second = CreateContext("/api/cart/lines", "203.0.113.11");
            first.Request.Headers.Cookie = $"{StorefrontCookieNames.CartToken}=local-cart-a";
            second.Request.Headers.Cookie = $"{StorefrontCookieNames.CartToken}=local-cart-a";

            Assert.Equal(
                StorefrontRateLimitIdentity.ResolveLocalCartActor(first),
                StorefrontRateLimitIdentity.ResolveLocalCartActor(second));
        }

        [Fact]
        public void StorefrontLocalCartRateLimitIdentity_FallsBackToRemoteIpAndIgnoresRawForwardedFor()
        {
            var first = CreateContext("/api/cart/lines", "203.0.113.10");
            var second = CreateContext("/api/cart/lines", "203.0.113.10");
            first.Request.Headers["X-Forwarded-For"] = "198.51.100.10";
            second.Request.Headers["X-Forwarded-For"] = "198.51.100.11";

            Assert.Equal(
                StorefrontRateLimitIdentity.ResolveLocalCartActor(first),
                StorefrontRateLimitIdentity.ResolveLocalCartActor(second));
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
            yield return [typeof(StorefrontScopedCurrencyController), nameof(StorefrontScopedCurrencyController.SetPreference), StorefrontRateLimitPolicyNames.Currency];
            yield return [typeof(StorefrontScopedCheckoutController), nameof(StorefrontScopedCheckoutController.Preview), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedCheckoutController), nameof(StorefrontScopedCheckoutController.PlaceOrder), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedNewsletterController), nameof(StorefrontScopedNewsletterController.Subscribe), StorefrontRateLimitPolicyNames.Newsletter];
            yield return [typeof(StorefrontScopedConsentController), nameof(StorefrontScopedConsentController.Save), StorefrontRateLimitPolicyNames.Newsletter];
            yield return [typeof(StorefrontScopedConsentController), nameof(StorefrontScopedConsentController.Revoke), StorefrontRateLimitPolicyNames.Newsletter];
            yield return [typeof(StorefrontScopedPaymentsController), nameof(StorefrontScopedPaymentsController.HandleProviderCallback), StorefrontRateLimitPolicyNames.Checkout];
            yield return [typeof(StorefrontScopedPaymentsController), nameof(StorefrontScopedPaymentsController.HandleWebhook), StorefrontRateLimitPolicyNames.Checkout];
        }

        private static string ReadStorefrontLocalEndpointSupportSource()
        {
            var root = FindStorefrontSupportRepositoryRoot();
            var endpointDirectory = Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "Endpoints");
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(endpointDirectory, "StorefrontLocalEndpointSupport*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private static DefaultHttpContext CreateContext(string path, string remoteIp)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            return context;
        }

        private static string FindStorefrontSupportRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
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
