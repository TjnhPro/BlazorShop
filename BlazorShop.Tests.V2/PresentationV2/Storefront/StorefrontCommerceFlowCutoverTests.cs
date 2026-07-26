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
            var activeStorefrontSources = new[]
            {
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"),
                ReadStorefrontApiClientSources(),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShell.razor"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Cart/CartView.razor"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountOrderList.razor"),
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountOrderDetail.razor"),
            };

            foreach (var source in activeStorefrontSources)
            {
                foreach (var retiredRoute in RetiredStorefrontRoutes)
                {
                    Assert.DoesNotContain(retiredRoute, source, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void StorefrontV2CheckoutAndAccountFlow_UsesCanonicalRoutes()
        {
            var apiClient = ReadStorefrontApiClientSources();
            var checkoutEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCheckoutEndpoints.cs");
            var checkoutShell = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShell.razor");

            Assert.Contains("StorefrontCartSessionRoute = StorefrontCartRoute + \"/session\"", apiClient, StringComparison.Ordinal);
            Assert.Contains("checkout/start", apiClient, StringComparison.Ordinal);
            Assert.Contains("checkout/place-order", apiClient, StringComparison.Ordinal);
            Assert.Contains("orders/current-user", apiClient, StringComparison.Ordinal);
            Assert.Contains("GetCustomerOrderReceiptAsync", apiClient, StringComparison.Ordinal);

            Assert.Contains("app.MapPost(\"/api/checkout/review\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/place-order\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("Actions.ReviewRoute", checkoutShell, StringComparison.Ordinal);
            Assert.Contains("Actions.PlaceOrderRoute", checkoutShell, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeStorefrontCartAndOrdersControllers_DoNotInjectLegacyCartServices()
        {
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCartController.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedOrdersController.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedPaymentsController.cs");

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

        [Fact]
        public void StorefrontV2CartClient_UsesRuntimeFacadeForActiveCartCrud()
        {
            var serviceCollection = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");
            var runtimeFacade = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeCartFacade.cs");
            var generatedAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontCartClient.cs");
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCartEndpoints.cs");
            var cartContracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/Contracts/StorefrontCartLocalContracts.cs");
            var cartEndpointSupport = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Cart.cs");
            var commonContracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/CommonContracts.cs");
            var cartComponents = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Cart/StorefrontCartView.razor")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/StorefrontLocalApiClient.cs");
            var cartOptions = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Cart/StorefrontCartViewOptions.cs");

            Assert.Contains("AddScoped<IStorefrontRuntimeCartFacade, StorefrontRuntimeCartFacade>", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeCartFacade", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("CreateOrResumeSessionAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("GetCartAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("AddLineAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("UpdateLineAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("RemoveLineAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("ClearAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("ValidateAsync", runtimeFacade, StringComparison.Ordinal);
            Assert.Contains("RecalculateAsync", runtimeFacade, StringComparison.Ordinal);

            Assert.Contains("AddScoped<GeneratedStorefrontCartClient>", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCartClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCartClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeCartFacade cartFacade", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("this.cartFacade.AddLineAsync", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("this.cartFacade.UpdateLineAsync", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("this.cartFacade.RemoveLineAsync", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("this.cartFacade.ClearAsync", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("this.cartFacade.RecalculateAsync", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("single auth-sensitive cart exception", generatedAdapter, StringComparison.Ordinal);
            Assert.Contains("this.manualClient.MergeCurrentCustomerCartAsync", generatedAdapter, StringComparison.Ordinal);

            Assert.Contains("app.MapGet(\"/api/cart\"", cartEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/cart/lines\"", cartEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/cart/recalculate\"", cartEndpoints, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalCartRecalculateRequest", cartContracts, StringComparison.Ordinal);
            Assert.Contains("int? StatusCode", commonContracts, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status409Conflict", cartEndpointSupport, StringComparison.Ordinal);
            Assert.Contains("ValidateLocalCartAntiforgeryAsync", cartEndpoints, StringComparison.Ordinal);
            Assert.Contains("Actions.CurrentCartRoute", cartComponents, StringComparison.Ordinal);
            Assert.Contains("\"/api/cart\"", cartOptions, StringComparison.Ordinal);
            Assert.DoesNotContain("localhost:5180", cartComponents, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2CheckoutAndPaymentClients_UseRuntimeFacadesForActiveGuestCheckout()
        {
            var serviceCollection = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");
            var checkoutFacade = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeCheckoutFacade.cs");
            var paymentFacade = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimePaymentFacade.cs");
            var checkoutAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontCheckoutClient.cs");
            var paymentAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontPaymentClient.cs");
            var checkoutEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCheckoutEndpoints.cs");
            var checkoutEndpointSupport = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs");
            var checkoutComponents = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShell.razor")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/StorefrontLocalApiClient.cs");
            var checkoutOptions = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShellOptions.cs");

            Assert.Contains("AddScoped<IStorefrontRuntimeCheckoutFacade, StorefrontRuntimeCheckoutFacade>", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontRuntimePaymentFacade, StorefrontRuntimePaymentFacade>", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeCheckoutFacade", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("PreviewAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("StartAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("LoadAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("UpdateAddressesAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("SelectShippingMethodAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("SelectPaymentMethodAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("ReviewAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("PlaceOrderAsync", checkoutFacade, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimePaymentFacade", paymentFacade, StringComparison.Ordinal);
            Assert.Contains("ListMethodsAsync", paymentFacade, StringComparison.Ordinal);
            Assert.Contains("GetAttemptAsync", paymentFacade, StringComparison.Ordinal);

            Assert.Contains("AddScoped<GeneratedStorefrontCheckoutClient>", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<GeneratedStorefrontPaymentClient>", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCheckoutClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCheckoutClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontPaymentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontPaymentClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.StartAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.UpdateAddressesAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.SelectShippingMethodAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.SelectPaymentMethodAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.ReviewAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.PlaceOrderAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("auth-sensitive checkout exception", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.manualClient.UpdateCheckoutAddressesAsync", checkoutAdapter, StringComparison.Ordinal);
            Assert.Contains("this.paymentFacade.ListMethodsAsync", paymentAdapter, StringComparison.Ordinal);
            Assert.Contains("this.paymentFacade.GetAttemptAsync", paymentAdapter, StringComparison.Ordinal);

            Assert.Contains("app.MapGet(\"/api/checkout\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/addresses\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/shipping-method\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/payment-method\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/review\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/place-order\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status409Conflict", checkoutEndpointSupport + checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("Actions.CurrentCheckoutRoute", checkoutComponents, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout", checkoutOptions, StringComparison.Ordinal);
            Assert.DoesNotContain("localhost:5180", checkoutComponents, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2AddressAndConsentClients_UseRuntimeFacadesForPublicAccountSupport()
        {
            var serviceCollection = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");
            var addressFacade = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeAddressFacade.cs");
            var consentFacade = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeConsentFacade.cs");
            var addressAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontAddressClient.cs");
            var consentAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontConsentClient.cs");
            var accountEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAccountEndpoints.cs");
            var consentEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontConsentEndpoints.cs");
            var sessionResolver = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSessionResolver.cs");
            var authClient = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontAuthClient.cs");

            Assert.Contains("AddScoped<IStorefrontRuntimeAddressFacade, StorefrontRuntimeAddressFacade>", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontRuntimeConsentFacade, StorefrontRuntimeConsentFacade>", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeAddressFacade", addressFacade, StringComparison.Ordinal);
            Assert.Contains("ListCountriesAsync", addressFacade, StringComparison.Ordinal);
            Assert.Contains("ListStatesAsync", addressFacade, StringComparison.Ordinal);
            Assert.Contains("GetConfigurationAsync", addressFacade, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeConsentFacade", consentFacade, StringComparison.Ordinal);
            Assert.Contains("GetCurrentAsync", consentFacade, StringComparison.Ordinal);
            Assert.Contains("SaveAsync", consentFacade, StringComparison.Ordinal);
            Assert.Contains("RevokeAsync", consentFacade, StringComparison.Ordinal);

            Assert.Contains("AddScoped<GeneratedStorefrontAddressClient>", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<GeneratedStorefrontConsentClient>", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontAddressClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontAddressClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontConsentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontConsentClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IStorefrontAddressClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IStorefrontConsentClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("this.addressFacade.ListCountriesAsync", addressAdapter, StringComparison.Ordinal);
            Assert.Contains("this.addressFacade.ListStatesAsync", addressAdapter, StringComparison.Ordinal);
            Assert.Contains("this.addressFacade.GetConfigurationAsync", addressAdapter, StringComparison.Ordinal);
            Assert.Contains("this.consentFacade.GetCurrentAsync", consentAdapter, StringComparison.Ordinal);
            Assert.Contains("this.consentFacade.SaveAsync", consentAdapter, StringComparison.Ordinal);
            Assert.Contains("this.consentFacade.RevokeAsync", consentAdapter, StringComparison.Ordinal);

            Assert.Contains("IStorefrontSessionResolver sessionResolver", accountEndpoints, StringComparison.Ordinal);
            Assert.Contains("ValidateLocalCartAntiforgeryAsync", accountEndpoints + consentEndpoints, StringComparison.Ordinal);
            Assert.Contains("BuildRefreshTokenCookieHeader", sessionResolver, StringComparison.Ordinal);
            Assert.Contains("CopySetCookieHeaders", sessionResolver, StringComparison.Ordinal);
            Assert.Contains("request.Headers.Authorization", authClient, StringComparison.Ordinal);
            Assert.Contains("request.Headers.TryAddWithoutValidation(\"Cookie\"", authClient, StringComparison.Ordinal);
        }

        private static string ReadStorefrontApiClientSources()
        {
            var root = FindRepositoryRoot();
            var servicesDirectory = Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "Services");
            return string.Join(
                Environment.NewLine,
                Directory.GetFiles(servicesDirectory, "StorefrontApi*.cs")
                    .Where(path => !path.EndsWith("StorefrontApiResult.cs", StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal)
                    .Select(File.ReadAllText));
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

        private static string FindRepositoryRoot()
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
    }
}
