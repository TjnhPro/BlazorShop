namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontV2WASMRuntimeFoundationTests
    {
        [Fact]
        public void WasmStartup_RegistersSameOriginClientWithoutCommerceNodeConfiguration()
        {
            var program = File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.Storefront.V2.WASM",
                "Program.cs"));

            Assert.Contains("AddStorefrontBrowserRuntime(builder.HostEnvironment)", program, StringComparison.Ordinal);
            Assert.DoesNotContain("new HttpClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalApiClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontAntiforgeryTokenReader", program, StringComparison.Ordinal);
            Assert.DoesNotContain("CommerceNode", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NodeKey", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NodeSecret", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refresh", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", program, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WasmProject_DoesNotReferenceServerRuntimeOrGeneratedStorefrontClient()
        {
            var root = RepositoryRoot();
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");
            var source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2.WASM"),
                        "*.*",
                        SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));

            Assert.DoesNotContain("BlazorShop.Storefront.Runtime", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Client", project, StringComparison.Ordinal);
            Assert.DoesNotContain("CommerceNodeBaseUrl", source, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRuntimeOptions", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Storefront.Runtime", source, StringComparison.Ordinal);
        }

        [Fact]
        public void WasmProjectIdentity_IsExplicitlyScopedToStorefrontV2()
        {
            var solution = ReadRepositoryFile("BlazorShop.sln");
            var hostProject = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");
            var testsProject = ReadRepositoryFile("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj");
            var wasmProject = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");

            Assert.Contains("BlazorShop.Storefront.V2.WASM", solution, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2.WASM.csproj", hostProject, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2.WASM.csproj", testsProject, StringComparison.Ordinal);
            Assert.Contains("<RootNamespace>BlazorShop.Storefront.V2.WASM</RootNamespace>", wasmProject, StringComparison.Ordinal);
            Assert.False(File.Exists(ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj")));
            Assert.DoesNotContain("BlazorShop.Storefront.WASM", solution, StringComparison.Ordinal);
        }

        [Fact]
        public void WasmProject_OwnsInteractiveRootComponentsUsedByStorefrontV2()
        {
            var imports = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/_Imports.razor");
            var hostProgram = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            foreach (var componentPath in new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor"
            })
            {
                Assert.True(File.Exists(ResolveRepositoryPath(componentPath)), $"{componentPath} must be compiled into the WASM client assembly.");
            }

            Assert.Contains("@namespace BlazorShop.Storefront.V2.WASM", imports, StringComparison.Ordinal);
            Assert.Contains("typeof(BlazorShop.Storefront.V2.WASM.Components.Account.StorefrontAccountApp).Assembly", hostProgram, StringComparison.Ordinal);
            Assert.False(Directory.EnumerateFiles(
                    ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components"),
                    "StorefrontCartView.razor",
                    SearchOption.AllDirectories)
                .Any());
            Assert.False(Directory.EnumerateFiles(
                    ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components"),
                    "StorefrontCheckoutShell.razor",
                    SearchOption.AllDirectories)
                .Any());
            Assert.False(Directory.EnumerateFiles(
                    ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components"),
                    "StorefrontAccountApp.razor",
                    SearchOption.AllDirectories)
                .Any());
        }

        [Fact]
        public void CartPage_HostsInteractiveWasmCartViewWithServerSnapshot()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor");

            Assert.Contains("<StorefrontCartView", page, StringComparison.Ordinal);
            Assert.DoesNotContain("<CartView", page, StringComparison.Ordinal);
            Assert.Contains("InitialCart=\"Context.Cart\"", page, StringComparison.Ordinal);
            Assert.Contains("InitialAlerts=\"Context.Alerts\"", page, StringComparison.Ordinal);
            Assert.Contains("DataMode=\"StorefrontFeatureDataMode.InitialSnapshot\"", page, StringComparison.Ordinal);
            Assert.Contains("Actions=\"StorefrontCartViewOptions.Actions\"", page, StringComparison.Ordinal);
            Assert.Contains("Classes=\"StorefrontCartViewOptions.Classes\"", page, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", page, StringComparison.Ordinal);
        }

        [Fact]
        public void CartWasmComponent_DelegatesSameOriginLocalCartEndpointsToBrowserController()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor");
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/Cart/StorefrontBrowserCartController.cs");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewOptions.cs");
            var behavior = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Cart/StorefrontCartBehavior.cs");

            Assert.Contains("GetAsync<StorefrontBrowserCart>(_actions.CurrentCartRoute", controller, StringComparison.Ordinal);
            Assert.Contains("ShouldFetchAfterHydration()", controller, StringComparison.Ordinal);
            Assert.Contains("StorefrontFeatureDataMode.InitialSnapshot => false", controller, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCartQuantityRequest, StorefrontBrowserCart>", controller, StringComparison.Ordinal);
            Assert.Contains("_actions.UpdateLineRoute(lineId)", controller, StringComparison.Ordinal);
            Assert.Contains("DeleteAsync<StorefrontBrowserCart>", controller, StringComparison.Ordinal);
            Assert.Contains("_actions.RemoveLineRoute(lineId)", controller, StringComparison.Ordinal);
            Assert.Contains("_actions.ClearCartRoute", controller, StringComparison.Ordinal);
            Assert.Contains("PublishCartChangedAsync(result.Data.Count", controller, StringComparison.Ordinal);
            Assert.Contains("IStorefrontBrowserCartController", component, StringComparison.Ordinal);
            Assert.Contains("CartController.HydrateAsync()", component, StringComparison.Ordinal);
            Assert.Contains("CartController.UpdateQuantityAsync(line.LineId, value)", component, StringComparison.Ordinal);
            Assert.Contains("CartController.RemoveLineAsync(line.LineId)", component, StringComparison.Ordinal);
            Assert.Contains("CartController.ClearAsync()", component, StringComparison.Ordinal);
            Assert.Contains("StateHasChanged();", component, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalApiClient", component, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontBrowserCartQuantityRequest", component, StringComparison.Ordinal);
            Assert.DoesNotContain("GetAsync<", component, StringComparison.Ordinal);
            Assert.DoesNotContain("PutJsonAsync<", component, StringComparison.Ordinal);
            Assert.DoesNotContain("DeleteAsync<", component, StringComparison.Ordinal);
            Assert.DoesNotContain("IServiceProvider", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-quantity", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-remove", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-clear", component, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/cart", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/cart\"", options, StringComparison.Ordinal);
            Assert.Contains("StorefrontCartActionDescriptor", behavior, StringComparison.Ordinal);
            Assert.Contains("StorefrontCartViewState", behavior, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", component, StringComparison.OrdinalIgnoreCase);

            var tokenReader = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/StorefrontAntiforgeryTokenReader.cs");
            var cartEventPublisher = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/StorefrontBrowserCartEventPublisher.cs");
            var interop = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/wwwroot/js/storefrontWasmInterop.js");
            Assert.Contains("IStorefrontBrowserCartEventPublisher", cartEventPublisher, StringComparison.Ordinal);
            Assert.Contains("PublishCartChangedAsync(int count", cartEventPublisher, StringComparison.Ordinal);
            Assert.DoesNotContain("IJSRuntime JS", component, StringComparison.Ordinal);
            Assert.DoesNotContain("IJSObjectReference", component, StringComparison.Ordinal);
            Assert.Contains("publishCartChanged", cartEventPublisher, StringComparison.Ordinal);
            Assert.Contains("./_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js", cartEventPublisher, StringComparison.Ordinal);
            Assert.Contains("./_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js", tokenReader, StringComparison.Ordinal);
            Assert.Contains("[data-storefront-cart-badge]", interop, StringComparison.Ordinal);
            Assert.Contains("blazorshop:cart-changed", interop, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCommerceScript_DoesNotOwnCartSummaryRefreshAfterPresentationBinderMigration()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var applicationScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");

            Assert.Contains("bindCartBadge", applicationScript, StringComparison.Ordinal);
            Assert.Contains("void cart.current()", applicationScript, StringComparison.Ordinal);
            Assert.DoesNotContain("refreshCartSummary", script, StringComparison.Ordinal);
            Assert.DoesNotContain("setInterval(refreshCartSummary", script, StringComparison.Ordinal);
            Assert.DoesNotContain("startBadgePolling", script, StringComparison.Ordinal);
            Assert.DoesNotContain("badgePollIntervalMs", script, StringComparison.Ordinal);
            Assert.DoesNotContain("data-storefront-cart-remove", script, StringComparison.Ordinal);
            Assert.DoesNotContain("data-storefront-cart-clear", script, StringComparison.Ordinal);
            Assert.DoesNotContain("data-storefront-cart-quantity", script, StringComparison.Ordinal);
            Assert.Contains("cartFeedbackSuppressUntil", script, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountHostPage_HostsInteractiveWasmAccountApp()
        {
            var host = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var route = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor");
            var pageService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Account/StorefrontAccountPageService.cs");
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            Assert.Contains("@page \"/account\"", route, StringComparison.Ordinal);
            Assert.Contains("@page \"/account/{*Path}\"", route, StringComparison.Ordinal);
            Assert.Contains("<StorefrontAccountApp", host, StringComparison.Ordinal);
            Assert.DoesNotContain("<AccountApp", host, StringComparison.Ordinal);
            Assert.Contains("Path=\"@Context.Path\"", host, StringComparison.Ordinal);
            Assert.Contains("AntiforgeryFieldName=\"@Context.AntiforgeryFieldName\"", host, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", host, StringComparison.Ordinal);
            Assert.DoesNotContain("@page", host, StringComparison.Ordinal);
            Assert.Contains("sessionResolver.GetCurrentUserAsync", pageService, StringComparison.Ordinal);
            Assert.Contains("StorefrontReturnUrl.BuildSignInUrl(CurrentReturnUrl(httpContext))", pageService, StringComparison.Ordinal);

            Assert.Contains("StorefrontAccountProfileEditor", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountAddressBook", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountOrderList", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountOrderDetail", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountChangePasswordForm", app, StringComparison.Ordinal);
            Assert.Contains("StorefrontFeatureDataMode.BrowserFetch", app, StringComparison.Ordinal);
            Assert.Contains("RouteDescriptor=\"StorefrontAccountViewOptions.RouteDescriptor\"", host, StringComparison.Ordinal);
            Assert.Contains("AccountRouteParser.Resolve(Path, RouteDescriptor)", app, StringComparison.Ordinal);
            Assert.DoesNotContain("string.Equals(normalized, \"profile\"", app, StringComparison.Ordinal);
            Assert.DoesNotContain("InitialProfile=\"_profile\"", host + app, StringComparison.Ordinal);
            Assert.DoesNotContain("GetCustomerProfileAsync", host, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountHost_UsesSingleShellWhileKeepingPageOwnedGuards()
        {
            var host = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var route = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor");
            var pageService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Account/StorefrontAccountPageService.cs");
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");
            var navigation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountNavigation.razor");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");

            Assert.Contains("new StorefrontPageDocument(\"Account\", RobotsIndex: false, RobotsFollow: false)", route, StringComparison.Ordinal);
            Assert.Contains("antiforgery.GetAndStoreTokens(httpContext)", pageService, StringComparison.Ordinal);
            Assert.Contains("StorefrontReturnUrl.Normalize(path + query, StorefrontRoutes.Account)", pageService, StringComparison.Ordinal);
            Assert.Contains("data-storefront-account-app", app, StringComparison.Ordinal);
            Assert.Contains("aria-label=\"Account navigation\"", navigation, StringComparison.Ordinal);
            Assert.Contains("/account/profile", options, StringComparison.Ordinal);
            Assert.Contains("/account/orders", options, StringComparison.Ordinal);
            Assert.Contains("/account/addresses", options, StringComparison.Ordinal);
            Assert.Contains("/account/change-password", options, StringComparison.Ordinal);
            Assert.Contains("NavigationItems=\"StorefrontAccountViewOptions.NavigationItems\"", host, StringComparison.Ordinal);
            Assert.Contains("NavigationClasses=\"StorefrontAccountViewOptions.NavigationClasses\"", host, StringComparison.Ordinal);

            foreach (var removedPage in new[]
            {
                "AccountProfilePage.razor",
                "AccountAddressesPage.razor",
                "AccountOrdersPage.razor",
                "AccountOrderDetailPage.razor",
                "AccountChangePasswordPage.razor",
            })
            {
                Assert.False(File.Exists(Path.Combine(RepositoryRoot(), "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "Pages", "WasmHost", "Account", removedPage)));
            }
        }

        [Fact]
        public void AccountWasmComponents_UseSameOriginLocalAccountEndpoints()
        {
            var profileComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountProfileEditor.razor");
            var addressesComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountAddressBook.razor");
            var ordersComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountOrderList.razor");
            var orderDetailComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountOrderDetail.razor");
            var passwordComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountChangePasswordForm.razor");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");
            var behavior = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs");
            var allComponents = profileComponent + addressesComponent + ordersComponent + orderDetailComponent + passwordComponent;

            Assert.Contains("<AntiforgeryToken />", profileComponent, StringComparison.Ordinal);
            Assert.Contains("<AntiforgeryToken />", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("<AntiforgeryToken />", passwordComponent, StringComparison.Ordinal);
            Assert.Contains("Passwords do not match.", passwordComponent, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserCustomerProfile>(Actions.LoadProfileRoute)", profileComponent, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCustomerProfileUpdateRequest, StorefrontBrowserCustomerProfile>(Actions.SaveProfileRoute", profileComponent, StringComparison.Ordinal);
            Assert.Contains("Actions.ChangePasswordRoute", passwordComponent, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/profile\"", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/change-password\"", options, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/addresses\"", options, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountProfileActionDescriptor", behavior, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountPasswordActionDescriptor", behavior, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountAddressActionDescriptor", behavior, StringComparison.Ordinal);
            Assert.Contains("GetAsync<IReadOnlyList<StorefrontBrowserCustomerAddress>>(Actions.CurrentAddressesRoute)", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<StorefrontBrowserCustomerAddressRequest, StorefrontBrowserCustomerAddress>", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("Actions.CreateAddressRoute", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("Actions.UpdateAddressRoute(addressId)", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("Actions.DeleteAddressRoute(addressId)", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("StorefrontAccountOrderActionDescriptor", behavior, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderList>(Actions.OrderListRoute(PageNumber))", ordersComponent, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderDetail>(route)", orderDetailComponent, StringComparison.Ordinal);
            Assert.Contains("Actions.OrderDetailRoute(OrderReference)", orderDetailComponent, StringComparison.Ordinal);
            Assert.Contains("Actions.ReceiptRoute(OrderReference)", orderDetailComponent, StringComparison.Ordinal);
            Assert.Contains("\"/api/account/orders?page={pageNumber}\"", options, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<object, StorefrontBrowserAccountCommandResult>", passwordComponent, StringComparison.Ordinal);
            Assert.Contains("DataMode == StorefrontFeatureDataMode.InitialSnapshot", profileComponent, StringComparison.Ordinal);
            Assert.Contains("DataMode == StorefrontFeatureDataMode.InitialSnapshot", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("DataMode == StorefrontFeatureDataMode.InitialSnapshot", ordersComponent, StringComparison.Ordinal);
            Assert.Contains("DataMode == StorefrontFeatureDataMode.InitialSnapshot", orderDetailComponent, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", allComponents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", allComponents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", allComponents, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AccountLocalEndpoints_ResolveCurrentCustomerServerSide()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var presentationAggregation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs");
            var accountEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationAccountEndpoints.cs");
            var support = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Account.cs");

            Assert.Contains("app.MapStorefrontApplication(", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentation();", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs"), StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationAccountEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/account/profile\"", accountEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPut(\"/api/account/profile\"", accountEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/account/addresses\"", accountEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/account/change-password\"", accountEndpoints, StringComparison.Ordinal);
            Assert.Contains("ResolveLocalCustomerSessionAsync", support, StringComparison.Ordinal);
            Assert.Contains("IStorefrontSessionResolver sessionResolver", accountEndpoints, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status401Unauthorized", support, StringComparison.Ordinal);
            Assert.DoesNotContain("customerId", accountEndpoints, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckoutPage_HostsInteractiveWasmCheckoutShellWithServerSnapshot()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");
            var pageService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageService.cs");
            var pageContext = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageContext.cs");

            Assert.Contains("<StorefrontCheckoutShell", page, StringComparison.Ordinal);
            Assert.DoesNotContain("<CheckoutShell", page, StringComparison.Ordinal);
            Assert.Contains("InitialState=\"Context.CheckoutState\"", page, StringComparison.Ordinal);
            Assert.Contains("DataMode=\"StorefrontFeatureDataMode.InitialSnapshot\"", page, StringComparison.Ordinal);
            Assert.Contains("ShowPanel=\"false\"", page, StringComparison.Ordinal);
            Assert.Contains("Actions=\"StorefrontCheckoutShellOptions.Actions\"", page, StringComparison.Ordinal);
            Assert.Contains("Classes=\"StorefrontCheckoutShellOptions.Classes\"", page, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", page, StringComparison.Ordinal);
            Assert.DoesNotContain("@page", page, StringComparison.Ordinal);
            Assert.Contains("StorefrontBrowserCheckoutState", pageContext, StringComparison.Ordinal);
            Assert.Contains("ToBrowserCheckoutState(checkoutResult.Value", pageService, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutWasmShell_UsesSameOriginLocalCheckoutEndpoints()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShellOptions.cs");

            Assert.Contains("GetAsync<StorefrontBrowserCheckoutState>(Actions.CurrentCheckoutRoute)", component, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<StorefrontBrowserCheckoutSelectionRequest, StorefrontBrowserCheckoutState>", component, StringComparison.Ordinal);
            Assert.Contains("Actions.ShippingMethodRoute", component, StringComparison.Ordinal);
            Assert.Contains("Actions.PaymentMethodRoute", component, StringComparison.Ordinal);
            Assert.Contains("Actions.ReviewRoute", component, StringComparison.Ordinal);
            Assert.Contains("Actions.PlaceOrderRoute", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/place-order\"", options, StringComparison.Ordinal);
            Assert.Contains("public bool ShowPanel { get; set; } = true;", component, StringComparison.Ordinal);
            Assert.Contains("DataMode != StorefrontFeatureDataMode.InitialSnapshot", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-shell", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-cart-version", component, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/checkout", component, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", component, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckoutLocalEndpoints_KeepCartTokenAndStaleVersionChecksServerSide()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var presentationAggregation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs");
            var checkoutEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCheckoutEndpoints.cs");
            var support = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs");
            var runtimeCheckoutFacade = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeCheckoutFacade.cs");
            var generatedCheckoutAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/GeneratedStorefrontCheckoutClient.cs");

            Assert.Contains("app.MapStorefrontApplication(", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentation();", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs"), StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationCheckoutEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/checkout\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/addresses\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/shipping-method\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/payment-method\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/review\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/place-order\"", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("ValidateLocalCheckoutCommandAsync", support, StringComparison.Ordinal);
            Assert.Contains("StorefrontCookieNames.CartToken", support, StringComparison.Ordinal);
            Assert.Contains("expectedCartVersion > 0 && expectedCartVersion != cartResolution.Cart.Version", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status409Conflict", support, StringComparison.Ordinal);
            Assert.Contains("Your cart changed. Review the latest cart and try checkout again.", support, StringComparison.Ordinal);
            Assert.Contains("ExpectedCheckoutVersion = request.ExpectedCheckoutVersion", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("IStorefrontRuntimeCheckoutFacade checkoutFacade", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("IAntiforgery antiforgery", checkoutEndpoints, StringComparison.Ordinal);
            Assert.Contains("StorefrontCookieNames.CartToken", checkoutEndpoints + support, StringComparison.Ordinal);
            Assert.Contains("string? bearerToken", runtimeCheckoutFacade, StringComparison.Ordinal);
            Assert.Contains("AuthenticationHeaderValue(\"Bearer\", bearerToken.Trim())", runtimeCheckoutFacade, StringComparison.Ordinal);
            Assert.Contains("this.checkoutFacade.UpdateAddressesAsync", generatedCheckoutAdapter, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutOrderPaymentContracts_DoNotUseBackendDtosOrExposeProviderCallbacks()
        {
            var source = string.Join(
                Environment.NewLine,
                new[]
                {
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/CheckoutContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/PaymentContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/OrderContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/IStorefrontCheckoutClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/IStorefrontPaymentClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/IStorefrontCustomerClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeCheckoutFacade.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimePaymentFacade.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/GeneratedStorefrontCustomerClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageService.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCheckoutEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Account.cs",
                }.Select(ReadRepositoryFile));

            Assert.Contains("StorefrontPublicPaymentMethod", source, StringComparison.Ordinal);
            Assert.Contains("StorefrontSelectedAttribute", source, StringComparison.Ordinal);
            Assert.DoesNotContain("<GetPaymentMethod>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SelectedAttributeDto", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Application.DTOs.Payment", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Application.CommerceNode.VariationTemplates", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models.Payment", source, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontPaymentCallbackRequest", source, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontPaymentWebhookRequest", source, StringComparison.Ordinal);
            Assert.DoesNotContain("HandleProviderCallback", source, StringComparison.Ordinal);
            Assert.DoesNotContain("HandleWebhook", source, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontProgram_DelegatesLocalBrowserApiMappingToEndpointExtensions()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            var presentationAggregation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs");

            var applicationBuilder = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs");

            Assert.Contains("app.UseStorefrontApplication();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontApplication(", program, StringComparison.Ordinal);
            Assert.Contains("app.UseStorefrontPresentation();", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentation();", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationAuthEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationPreferenceEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationCartEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationAccountEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationCheckoutEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationConsentEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationSeoEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationMediaEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("app.MapStaticAssets();", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("app.MapDefaultEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapRazorComponents<StorefrontApp>()", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("components.AddInteractiveWebAssemblyRenderMode();", applicationBuilder, StringComparison.Ordinal);

            Assert.DoesNotContain("app.MapGet(\"/api/cart\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapGet(\"/api/account/profile\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapGet(\"/api/checkout\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapGet(\"/api/consent/current\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapGet(\"/media/products", program, StringComparison.Ordinal);
            Assert.DoesNotContain("ProxyCommerceNodeMediaAsync", program, StringComparison.Ordinal);
        }

        private static string RepositoryRoot()
        {
            var current = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "BlazorShop.sln")))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }

            throw new DirectoryNotFoundException("Could not find repository root.");
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
            return File.ReadAllText(ResolveRepositoryPath(relativePath));
        }

        private static string ResolveRepositoryPath(string relativePath)
        {
            return Path.Combine(
                RepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

    }
}
