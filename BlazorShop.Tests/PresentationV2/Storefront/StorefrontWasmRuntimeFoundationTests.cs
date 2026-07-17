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
