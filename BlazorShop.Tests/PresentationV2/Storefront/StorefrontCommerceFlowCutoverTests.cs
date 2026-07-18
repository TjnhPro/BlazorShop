namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontCommerceFlowCutoverTests
    {
        private static readonly string[] RetiredStorefrontRoutes =
        [
            "cart/save-checkout",
            "orders/confirm",
            "orders/current-user/items",
            "payments/paypal/capture",
        ];

        [Fact]
        public void StorefrontV2BrowserSurface_DoesNotCallRetiredCommerceNodeRoutes()
        {
            var activeStorefrontFiles = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Checkout/StorefrontCheckoutShell.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Cart/StorefrontCartView.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountOrderList.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountOrderDetail.razor",
            };

            foreach (var relativePath in activeStorefrontFiles)
            {
                var source = ReadRepositoryFile(relativePath);
                foreach (var retiredRoute in RetiredStorefrontRoutes)
                {
                    Assert.DoesNotContain(retiredRoute, source, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void StorefrontV2CheckoutAndAccountFlow_UsesCanonicalRoutes()
        {
            var apiClient = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var checkoutShell = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Checkout/StorefrontCheckoutShell.razor");

            Assert.Contains("StorefrontCartSessionRoute = StorefrontCartRoute + \"/session\"", apiClient, StringComparison.Ordinal);
            Assert.Contains("checkout/start", apiClient, StringComparison.Ordinal);
            Assert.Contains("checkout/place-order", apiClient, StringComparison.Ordinal);
            Assert.Contains("orders/current-user", apiClient, StringComparison.Ordinal);
            Assert.Contains("GetCustomerOrderReceiptAsync", apiClient, StringComparison.Ordinal);

            Assert.Contains("app.MapPost(\"/api/checkout/review\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/place-order\"", program, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/review\"", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/place-order\"", checkoutShell, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeStorefrontCartAndOrdersControllers_DoNotInjectLegacyCartServices()
        {
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");

            Assert.DoesNotContain("StorefrontScopedCartController(\r\n            ICartService", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontScopedOrdersController(\r\n            ICartService", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontScopedOrdersController(\r\n            IOrderQueryService", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveCheckout(", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("ConfirmOrder(", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("GetCurrentUserOrderItems(", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("CapturePayPal(", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("paypal/capture", controller, StringComparison.Ordinal);
            Assert.DoesNotContain("IPayPalPaymentService payPalPaymentService", controller, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeRuntime_DoesNotRegisterLegacyCartOrderFlowServices()
        {
            var dependencyInjection = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs");

            Assert.DoesNotContain("AddScoped<ICartService, CartService>", dependencyInjection, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IOrderQueryService, CommerceNodeOrderQueryService>", dependencyInjection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCartService, StorefrontCartService>", dependencyInjection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCheckoutService, StorefrontCheckoutService>", dependencyInjection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IOrderPlacementService, OrderPlacementService>", dependencyInjection, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeRuntime_DoesNotRegisterLegacyPaymentHandlers()
        {
            var dependencyInjection = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs");

            Assert.DoesNotContain("AddScoped<IPaymentHandler", dependencyInjection, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IPaymentHandlerResolver", dependencyInjection, StringComparison.Ordinal);
            Assert.DoesNotContain("PaymentHandlerResolver", dependencyInjection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IPaymentAttemptService, PaymentAttemptService>", dependencyInjection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontPaymentProvider, CodStorefrontPaymentProvider>", dependencyInjection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontPaymentProviderResolver, StorefrontPaymentProviderResolver>", dependencyInjection, StringComparison.Ordinal);
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
