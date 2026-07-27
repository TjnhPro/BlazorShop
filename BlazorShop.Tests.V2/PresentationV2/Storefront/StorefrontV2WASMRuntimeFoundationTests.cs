namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Components.Browser;

    using Xunit;

    public sealed class StorefrontV2WASMRuntimeFoundationTests
    {
        [Fact]
        public async Task GetAsync_UsesSameOriginRelativeRouteWithoutAntiforgeryHeader()
        {
            var handler = new RecordingHandler(new { count = 2 });
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var tokenReader = new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"));
            var client = new StorefrontLocalApiClient(httpClient, tokenReader);

            var result = await client.GetAsync<CartSummary>("/api/cart");

            Assert.True(result.Success);
            Assert.Equal(2, result.Data?.Count);
            Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
            Assert.Equal("https://storefront.example/api/cart", handler.LastRequest?.RequestUri?.ToString());
            Assert.False(handler.LastRequest?.Headers.Contains("X-CSRF-TOKEN"));
            Assert.Equal(0, tokenReader.ReadCount);
        }

        [Fact]
        public async Task MutatingJsonRequest_AddsAntiforgeryHeader()
        {
            var handler = new RecordingHandler(new { success = true });
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var tokenReader = new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"));
            var client = new StorefrontLocalApiClient(httpClient, tokenReader);

            var result = await client.PutJsonAsync<object, MutationResult>("api/cart/lines/4f0c0f4b-9f54-4f57-a3e4-111111111111", new { quantity = 3 });

            Assert.True(result.Success);
            Assert.True(result.Data?.Success);
            Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
            Assert.Equal("csrf-token", handler.LastRequest?.Headers.GetValues("X-CSRF-TOKEN").Single());
            Assert.Equal("application/json", handler.LastRequest?.Content?.Headers.ContentType?.MediaType);
            Assert.Equal(1, tokenReader.ReadCount);
        }

        [Theory]
        [InlineData("https://commerce-node.example/api/cart")]
        [InlineData("//commerce-node.example/api/cart")]
        public async Task LocalApiClient_RejectsAbsoluteOrProtocolRelativeRoutes(string route)
        {
            var handler = new RecordingHandler(new { success = true });
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            await Assert.ThrowsAsync<ArgumentException>(() => client.GetAsync<object>(route));
            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task LocalApiClient_HandlesEmptySuccessBodyWithUnknownLength()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, new UnknownLengthStringContent(string.Empty));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            var result = await client.DeleteAsync<MutationResult>("/api/cart");

            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Null(result.Data);
            Assert.Null(result.Error);
            Assert.Equal(string.Empty, result.Message);
        }

        [Fact]
        public async Task LocalApiClient_PreservesStructuredErrorDetails()
        {
            var fieldErrors = new Dictionary<string, string[]>
            {
                ["email"] = ["Email is invalid."],
            };
            var errorBody = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse(
                    "Email is invalid.",
                    "checkout.validation",
                    "trace-123",
                    fieldErrors,
                    false,
                    StatusCode: 422),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new RecordingHandler(
                HttpStatusCode.UnprocessableEntity,
                new StringContent(errorBody, Encoding.UTF8, "application/json"));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            var result = await client.PostJsonAsync<object, MutationResult>("/api/checkout/review", new { accepted = false });

            Assert.False(result.Success);
            Assert.Equal("Email is invalid.", result.Message);
            Assert.NotNull(result.Error);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Error.StatusCode);
            Assert.Equal("checkout.validation", result.Error.Code);
            Assert.Equal("trace-123", result.Error.TraceId);
            Assert.False(result.Error.Retryable);
            Assert.Equal("Email is invalid.", result.Error.FieldErrors["email"].Single());
        }

        [Fact]
        public async Task LocalApiClient_InvalidErrorBodyFallsBackToStatusDefault()
        {
            var handler = new RecordingHandler(
                HttpStatusCode.RequestTimeout,
                new StringContent("<html>timeout</html>", Encoding.UTF8, "text/html"));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            var result = await client.GetAsync<MutationResult>("/api/cart");

            Assert.False(result.Success);
            Assert.Equal("The request timed out. Try again.", result.Message);
            Assert.NotNull(result.Error);
            Assert.Equal("timeout", result.Error.Code);
            Assert.True(result.Error.Retryable);
            Assert.Empty(result.Error.FieldErrors);
        }

        [Fact]
        public void WasmStartup_RegistersSameOriginClientWithoutCommerceNodeConfiguration()
        {
            var program = File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.Storefront.V2.WASM",
                "Program.cs"));

            Assert.Contains("builder.HostEnvironment.BaseAddress", program, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalApiClient", program, StringComparison.Ordinal);
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
        public void CartWasmComponent_UsesSameOriginLocalCartEndpoints()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewOptions.cs");
            var behavior = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Cart/StorefrontCartBehavior.cs");

            Assert.Contains("GetAsync<StorefrontBrowserCart>(Actions.CurrentCartRoute)", component, StringComparison.Ordinal);
            Assert.Contains("ShouldFetchAfterHydration()", component, StringComparison.Ordinal);
            Assert.Contains("StorefrontFeatureDataMode.InitialSnapshot => false", component, StringComparison.Ordinal);
            Assert.Contains("await LoadCartAsync();", component, StringComparison.Ordinal);
            Assert.Contains("await PublishCartChangedAsync(_cart.Count);", component, StringComparison.Ordinal);
            Assert.Contains("StateHasChanged();", component, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCartQuantityRequest, StorefrontBrowserCart>", component, StringComparison.Ordinal);
            Assert.Contains("Actions.UpdateLineRoute(line.LineId)", component, StringComparison.Ordinal);
            Assert.Contains("DeleteAsync<StorefrontBrowserCart>(Actions.RemoveLineRoute(line.LineId))", component, StringComparison.Ordinal);
            Assert.Contains("DeleteAsync<StorefrontBrowserCart>(Actions.ClearCartRoute)", component, StringComparison.Ordinal);
            Assert.Contains("IsMutationBusy(line.LineId)", component, StringComparison.Ordinal);
            Assert.Contains("IsDisabled(line.LineId)", component, StringComparison.Ordinal);
            Assert.Contains("_apiClient is null || _clearing || _busyLineId.HasValue", component, StringComparison.Ordinal);
            Assert.Contains("!Lines.Any(candidate => candidate.LineId == line.LineId)", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-quantity", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-remove", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-clear", component, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/api/cart", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/cart\"", options, StringComparison.Ordinal);
            Assert.Contains("StorefrontCartActionDescriptor", behavior, StringComparison.Ordinal);
            Assert.Contains("StorefrontCartViewState", behavior, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", component, StringComparison.OrdinalIgnoreCase);

            var tokenReader = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/StorefrontAntiforgeryTokenReader.cs");
            var interop = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontWasmInterop.js");
            Assert.Contains("publishCartChanged", component, StringComparison.Ordinal);
            Assert.Contains("./js/storefrontWasmInterop.js", component, StringComparison.Ordinal);
            Assert.Contains("./js/storefrontWasmInterop.js", tokenReader, StringComparison.Ordinal);
            Assert.DoesNotContain("_content/BlazorShop.Storefront.Components", component + tokenReader, StringComparison.Ordinal);
            Assert.Contains("[data-storefront-cart-badge]", interop, StringComparison.Ordinal);
            Assert.Contains("blazorshop:cart-changed", interop, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCommerceScript_DoesNotPollCartSummaryAfterWasmCartMigration()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("refreshCartSummary();", script, StringComparison.Ordinal);
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
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            Assert.Contains("@page \"/account\"", host, StringComparison.Ordinal);
            Assert.Contains("@page \"/account/{*Path}\"", host, StringComparison.Ordinal);
            Assert.Contains("<StorefrontAccountApp", host, StringComparison.Ordinal);
            Assert.DoesNotContain("<AccountApp", host, StringComparison.Ordinal);
            Assert.Contains("Path=\"@Path\"", host, StringComparison.Ordinal);
            Assert.Contains("AntiforgeryFieldName=\"@_antiforgeryFieldName\"", host, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", host, StringComparison.Ordinal);
            Assert.Contains("SessionResolver.GetCurrentUserAsync()", host, StringComparison.Ordinal);
            Assert.Contains("StorefrontReturnUrl.BuildSignInUrl(CurrentReturnUrl())", host, StringComparison.Ordinal);

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
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");
            var navigation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountNavigation.razor");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewOptions.cs");

            Assert.Contains("<meta name=\"robots\" content=\"noindex,nofollow\" />", host, StringComparison.Ordinal);
            Assert.Contains("Antiforgery.GetAndStoreTokens(HttpContext)", host, StringComparison.Ordinal);
            Assert.Contains("NavigationManager.ToBaseRelativePath(NavigationManager.Uri)", host, StringComparison.Ordinal);
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
            var accountEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAccountEndpoints.cs");
            var support = ReadStorefrontLocalEndpointSupportSource();

            Assert.Contains("app.MapStorefrontAccountEndpoints();", program, StringComparison.Ordinal);
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
            var checkoutEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCheckoutEndpoints.cs");
            var support = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs");
            var apiClient = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Checkout.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiTransport.cs");

            Assert.Contains("app.MapStorefrontPresentationCheckoutEndpoints();", program, StringComparison.Ordinal);
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
            Assert.Contains("string? bearerToken = null", apiClient, StringComparison.Ordinal);
            Assert.Contains("message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(\"Bearer\", bearerToken)", apiClient, StringComparison.Ordinal);
            Assert.Contains("\"Unable to update checkout address right now.\",", apiClient, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutOrderPaymentContracts_DoNotUseBackendDtosOrExposeProviderCallbacks()
        {
            var source = string.Join(
                Environment.NewLine,
                new[]
                {
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/CheckoutContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/PaymentContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/OrderContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/IStorefrontCheckoutClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/IStorefrontPaymentClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/IStorefrontCustomerClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Checkout.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Payment.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Customer.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageService.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCheckoutEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Account.cs",
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

            Assert.Contains("app.MapStorefrontPresentationAuthEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontAuthFormEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentationCartEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontAccountEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentationCheckoutEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontConsentEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentationSeoEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontMediaEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStaticAssets();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapDefaultEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapRazorComponents<StorefrontApp>()", program, StringComparison.Ordinal);
            Assert.Contains(".AddInteractiveWebAssemblyRenderMode()", program, StringComparison.Ordinal);

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

        private sealed record CartSummary(int Count);

        private sealed record MutationResult(bool Success);

        private sealed class StubAntiforgeryTokenReader : IStorefrontAntiforgeryTokenReader
        {
            private readonly StorefrontAntiforgeryToken? _token;

            public StubAntiforgeryTokenReader(StorefrontAntiforgeryToken? token)
            {
                _token = token;
            }

            public int ReadCount { get; private set; }

            public ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default)
            {
                ReadCount++;
                return ValueTask.FromResult(_token);
            }
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpContent? _content;
            private readonly object? _response;
            private readonly HttpStatusCode _statusCode;

            public RecordingHandler(object response)
            {
                _response = response;
                _statusCode = HttpStatusCode.OK;
            }

            public RecordingHandler(HttpStatusCode statusCode, HttpContent content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;

                var content = _content;
                if (content is null)
                {
                    var json = JsonSerializer.Serialize(_response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = content,
                    RequestMessage = request,
                });
            }
        }

        private sealed class UnknownLengthStringContent : HttpContent
        {
            private readonly byte[] _bytes;

            public UnknownLengthStringContent(string value)
            {
                _bytes = Encoding.UTF8.GetBytes(value);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                return stream.WriteAsync(_bytes, 0, _bytes.Length);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
    }
}
