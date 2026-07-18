namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Components.Browser;

    using Xunit;

    public sealed class StorefrontWasmRuntimeFoundationTests
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
        public void WasmStartup_RegistersSameOriginClientWithoutCommerceNodeConfiguration()
        {
            var program = File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.Storefront.WASM",
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
        public void CartPage_HostsInteractiveWasmCartViewWithServerSnapshot()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CartPage.razor");

            Assert.Contains("<StorefrontCartView", page, StringComparison.Ordinal);
            Assert.Contains("InitialCart=\"_cart\"", page, StringComparison.Ordinal);
            Assert.Contains("InitialAlerts=\"_alerts\"", page, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", page, StringComparison.Ordinal);
        }

        [Fact]
        public void CartWasmComponent_UsesSameOriginLocalCartEndpoints()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Cart/StorefrontCartView.razor");

            Assert.Contains("GetAsync<StorefrontBrowserCart>(\"/api/cart\")", component, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCartQuantityRequest, StorefrontBrowserCart>", component, StringComparison.Ordinal);
            Assert.Contains("DeleteAsync<StorefrontBrowserCart>($\"/api/cart/lines/{line.LineId:D}\")", component, StringComparison.Ordinal);
            Assert.Contains("DeleteAsync<StorefrontBrowserCart>(\"/api/cart\")", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-quantity", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-remove", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-clear", component, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", component, StringComparison.OrdinalIgnoreCase);

            var interop = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/wwwroot/js/storefrontWasmInterop.js");
            Assert.Contains("publishCartChanged", component, StringComparison.Ordinal);
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
        }

        [Fact]
        public void AccountPages_HostInteractiveWasmAccountComponentsWithServerSnapshots()
        {
            var profilePage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountProfilePage.razor");
            var addressesPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountAddressesPage.razor");
            var ordersPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrdersPage.razor");
            var orderDetailPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrderDetailPage.razor");
            var passwordPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountChangePasswordPage.razor");

            Assert.Contains("<AccountProfileEditor", profilePage, StringComparison.Ordinal);
            Assert.Contains("InitialProfile=\"_profile\"", profilePage, StringComparison.Ordinal);
            Assert.Contains("<AccountAddressBook", addressesPage, StringComparison.Ordinal);
            Assert.Contains("InitialAddresses=\"_addresses\"", addressesPage, StringComparison.Ordinal);
            Assert.Contains("<AccountOrderList", ordersPage, StringComparison.Ordinal);
            Assert.Contains("InitialOrders=\"_orders\"", ordersPage, StringComparison.Ordinal);
            Assert.Contains("<AccountOrderDetail", orderDetailPage, StringComparison.Ordinal);
            Assert.Contains("InitialOrder=\"_order\"", orderDetailPage, StringComparison.Ordinal);
            Assert.Contains("<AccountChangePasswordForm", passwordPage, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", profilePage + addressesPage + ordersPage + orderDetailPage + passwordPage, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountWasmComponents_UseSameOriginLocalAccountEndpoints()
        {
            var profileComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountProfileEditor.razor");
            var addressesComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountAddressBook.razor");
            var ordersComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountOrderList.razor");
            var orderDetailComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountOrderDetail.razor");
            var passwordComponent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Account/AccountChangePasswordForm.razor");
            var allComponents = profileComponent + addressesComponent + ordersComponent + orderDetailComponent + passwordComponent;

            Assert.Contains("GetAsync<StorefrontBrowserCustomerProfile>(\"/api/account/profile\")", profileComponent, StringComparison.Ordinal);
            Assert.Contains("PutJsonAsync<StorefrontBrowserCustomerProfileUpdateRequest, StorefrontBrowserCustomerProfile>(\"/api/account/profile\"", profileComponent, StringComparison.Ordinal);
            Assert.Contains("GetAsync<IReadOnlyList<StorefrontBrowserCustomerAddress>>(\"/api/account/addresses\")", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<StorefrontBrowserCustomerAddressRequest, StorefrontBrowserCustomerAddress>", addressesComponent, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderList>", ordersComponent, StringComparison.Ordinal);
            Assert.Contains("GetAsync<StorefrontBrowserAccountOrderDetail>", orderDetailComponent, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<object, StorefrontBrowserAccountCommandResult>", passwordComponent, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", allComponents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", allComponents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", allComponents, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AccountLocalEndpoints_ResolveCurrentCustomerServerSide()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            Assert.Contains("app.MapGet(\"/api/account/profile\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPut(\"/api/account/profile\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/account/addresses\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/account/change-password\"", program, StringComparison.Ordinal);
            Assert.Contains("ResolveLocalCustomerSessionAsync", program, StringComparison.Ordinal);
            Assert.Contains("IStorefrontSessionResolver sessionResolver", program, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status401Unauthorized", program, StringComparison.Ordinal);
            Assert.DoesNotContain("customerId", program.Substring(program.IndexOf("app.MapGet(\"/api/account/profile\"", StringComparison.Ordinal)), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckoutPage_HostsInteractiveWasmCheckoutShellWithServerSnapshot()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CheckoutPage.razor");
            var codeBehind = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CheckoutPage.razor.cs");

            Assert.Contains("<StorefrontCheckoutShell", page, StringComparison.Ordinal);
            Assert.Contains("InitialState=\"CheckoutState\"", page, StringComparison.Ordinal);
            Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", page, StringComparison.Ordinal);
            Assert.Contains("StorefrontBrowserCheckoutState", codeBehind, StringComparison.Ordinal);
            Assert.Contains("ToBrowserCheckoutState(checkoutSession)", codeBehind, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutWasmShell_UsesSameOriginLocalCheckoutEndpoints()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Checkout/StorefrontCheckoutShell.razor");

            Assert.Contains("GetAsync<StorefrontBrowserCheckoutState>(\"/api/checkout\")", component, StringComparison.Ordinal);
            Assert.Contains("PostJsonAsync<StorefrontBrowserCheckoutSelectionRequest, StorefrontBrowserCheckoutState>", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/shipping-method\"", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/payment-method\"", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/review\"", component, StringComparison.Ordinal);
            Assert.Contains("\"/api/checkout/place-order\"", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-shell", component, StringComparison.Ordinal);
            Assert.Contains("data-storefront-checkout-cart-version", component, StringComparison.Ordinal);
            Assert.DoesNotContain("api/storefront/stores", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", component, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", component, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckoutLocalEndpoints_KeepCartTokenAndStaleVersionChecksServerSide()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            Assert.Contains("app.MapGet(\"/api/checkout\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/addresses\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/shipping-method\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/payment-method\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/review\"", program, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/place-order\"", program, StringComparison.Ordinal);
            Assert.Contains("ValidateLocalCheckoutCommandAsync", program, StringComparison.Ordinal);
            Assert.Contains("StorefrontCookieNames.CartToken", program, StringComparison.Ordinal);
            Assert.Contains("expectedCartVersion > 0 && expectedCartVersion != cartResult.Data.Version", program, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status409Conflict", program, StringComparison.Ordinal);
            Assert.Contains("Your cart changed. Review the latest cart and try checkout again.", program, StringComparison.Ordinal);
            Assert.Contains("ExpectedCheckoutVersion = request.ExpectedCheckoutVersion", program, StringComparison.Ordinal);
            Assert.Contains("IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)", program, StringComparison.Ordinal);
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

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
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
            private readonly object _response;

            public RecordingHandler(object response)
            {
                _response = response;
            }

            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;

                var json = JsonSerializer.Serialize(_response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    RequestMessage = request,
                });
            }
        }
    }
}
