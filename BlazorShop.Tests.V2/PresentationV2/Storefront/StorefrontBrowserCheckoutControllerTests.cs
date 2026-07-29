namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Browser;
    using BlazorShop.Storefront.Browser.Checkout;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Components.Headless.Checkout;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public sealed class StorefrontBrowserCheckoutControllerTests
    {
        [Fact]
        public async Task HydrateAsync_LoadsCheckoutState_WhenBrowserFetchMode()
        {
            var sessionId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateState(sessionId, cartVersion: 2, checkoutVersion: 3));
            var controller = CreateController(handler);
            controller.Initialize(StorefrontBrowserCheckoutDefaults.EmptyState("empty"), showPanel: true, StorefrontFeatureDataMode.BrowserFetch, Actions);

            var changed = await controller.HydrateAsync();

            Assert.True(changed);
            Assert.Equal(sessionId, controller.State.Checkout.CheckoutSessionId);
            Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/checkout", handler.Requests.Single().RequestUri?.ToString());
        }

        [Fact]
        public async Task SelectShippingAsync_CreatesSelectionRequestWithExpectedCartVersion()
        {
            var sessionId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateState(sessionId, cartVersion: 8, checkoutVersion: 4));
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 7, checkoutVersion: 4), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.SelectShippingAsync("ground");

            Assert.True(changed);
            Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/checkout/shipping-method", handler.Requests.Single().RequestUri?.ToString());
            Assert.Contains($"\"checkoutSessionId\":\"{sessionId:D}\"", handler.RequestBodies.Single(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"expectedCartVersion\":7", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Contains("\"key\":\"ground\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task SelectPaymentAsync_CreatesSelectionRequest()
        {
            var sessionId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateState(sessionId, cartVersion: 5, checkoutVersion: 2));
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 5, checkoutVersion: 2), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.SelectPaymentAsync("cod");

            Assert.True(changed);
            Assert.Equal("https://storefront.example/api/checkout/payment-method", handler.Requests.Single().RequestUri?.ToString());
            Assert.Contains("\"key\":\"cod\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ReviewAsync_CreatesReviewRequestWithTermsAccepted()
        {
            var sessionId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateState(sessionId, cartVersion: 6, checkoutVersion: 3));
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 6, checkoutVersion: 2), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.ReviewAsync();

            Assert.True(changed);
            Assert.Equal("https://storefront.example/api/checkout/review", handler.Requests.Single().RequestUri?.ToString());
            Assert.Contains("\"termsAccepted\":true", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Contains("\"expectedCartVersion\":6", handler.RequestBodies.Single(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task PlaceOrderAsync_CreatesPlaceOrderRequestAndReturnsRedirect()
        {
            var sessionId = Guid.NewGuid();
            var handler = new QueueingHandler(new StorefrontBrowserCheckoutPlaceOrderResult(true, "Placed", "ORDER-1", "/orders/ORDER-1"));
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 6, checkoutVersion: 9), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var outcome = await controller.PlaceOrderAsync();

            Assert.True(outcome.Changed);
            Assert.Equal("/orders/ORDER-1", outcome.RedirectUrl);
            Assert.Equal("https://storefront.example/api/checkout/place-order", handler.Requests.Single().RequestUri?.ToString());
            Assert.Contains($"\"checkoutSessionId\":\"{sessionId:D}\"", handler.RequestBodies.Single(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"expectedCheckoutVersion\":9", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Contains("\"expectedCartVersion\":6", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Contains("\"idempotencyKey\":\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task PlaceOrderAsync_KeepsIdempotencyKeyForRetryWithinSameSession()
        {
            var sessionId = Guid.NewGuid();
            var error = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse("Try again.", "checkout.retry", null, null, true, 503),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new QueueingHandler(
                new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.ServiceUnavailable,
                new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.ServiceUnavailable);
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 6, checkoutVersion: 9), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await controller.PlaceOrderAsync();
            await controller.PlaceOrderAsync();

            Assert.Equal(ExtractIdempotencyKey(handler.RequestBodies[0]), ExtractIdempotencyKey(handler.RequestBodies[1]));
        }

        [Fact]
        public async Task PlaceOrderAsync_RotatesIdempotencyKeyWhenCheckoutSessionChanges()
        {
            var firstSessionId = Guid.NewGuid();
            var secondSessionId = Guid.NewGuid();
            var error = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse("Try again.", "checkout.retry", null, null, true, 503),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new QueueingHandler(
                new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.ServiceUnavailable,
                new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.ServiceUnavailable);
            var controller = CreateController(handler);
            controller.Initialize(CreateState(firstSessionId, cartVersion: 6, checkoutVersion: 9), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await controller.PlaceOrderAsync();
            controller.Initialize(CreateState(secondSessionId, cartVersion: 1, checkoutVersion: 1), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);
            await controller.PlaceOrderAsync();

            Assert.NotEqual(ExtractIdempotencyKey(handler.RequestBodies[0]), ExtractIdempotencyKey(handler.RequestBodies[1]));
            Assert.Contains($"\"checkoutSessionId\":\"{secondSessionId:D}\"", handler.RequestBodies[1], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PlaceOrderAsync_RotatesIdempotencyKeyAfterSuccessfulOrderPlacement()
        {
            var sessionId = Guid.NewGuid();
            var handler = new QueueingHandler(
                new StorefrontBrowserCheckoutPlaceOrderResult(true, "Placed", "ORDER-1", "/orders/ORDER-1"),
                new StorefrontBrowserCheckoutPlaceOrderResult(true, "Placed", "ORDER-1", "/orders/ORDER-1"));
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 6, checkoutVersion: 9), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await controller.PlaceOrderAsync();
            await controller.PlaceOrderAsync();

            Assert.NotEqual(ExtractIdempotencyKey(handler.RequestBodies[0]), ExtractIdempotencyKey(handler.RequestBodies[1]));
        }

        [Fact]
        public void Initialize_AcceptsNewerCheckoutSnapshot()
        {
            var sessionId = Guid.NewGuid();
            var controller = CreateController(new QueueingHandler(CreateState(sessionId, cartVersion: 1, checkoutVersion: 1)));
            controller.Initialize(CreateState(sessionId, cartVersion: 1, checkoutVersion: 1), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            controller.Initialize(CreateState(sessionId, cartVersion: 3, checkoutVersion: 4), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            Assert.Equal(3, controller.State.Checkout.CartVersion);
            Assert.Equal(4, controller.State.Checkout.CheckoutVersion);
        }

        [Fact]
        public async Task PlaceOrderAsync_MapsFailureToErrorWithoutRedirect()
        {
            var sessionId = Guid.NewGuid();
            var error = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse("Payment method is unavailable.", "checkout.payment", null, null, false, 422),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new QueueingHandler(new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.UnprocessableEntity);
            var controller = CreateController(handler);
            controller.Initialize(CreateState(sessionId, cartVersion: 6, checkoutVersion: 9), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var outcome = await controller.PlaceOrderAsync();

            Assert.True(outcome.Changed);
            Assert.Null(outcome.RedirectUrl);
            Assert.Equal("Payment method is unavailable.", controller.State.Error);
            Assert.False(controller.State.Loading);
        }

        [Fact]
        public async Task RefreshAsync_MapsApiErrorToCheckoutError()
        {
            var error = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse("Checkout changed.", "checkout.conflict", null, null, false, 409),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new QueueingHandler(new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.Conflict);
            var controller = CreateController(handler);
            controller.Initialize(StorefrontBrowserCheckoutDefaults.EmptyState("empty"), showPanel: true, StorefrontFeatureDataMode.BrowserFetch, Actions);

            var changed = await controller.RefreshAsync();

            Assert.True(changed);
            Assert.Equal("Checkout changed.", controller.State.Error);
            Assert.False(controller.State.Loading);
        }

        [Fact]
        public async Task RefreshAsync_ExceptionResetsLoading()
        {
            var sessionId = Guid.NewGuid();
            var controller = CreateController(new FailingHandler());
            controller.Initialize(CreateState(sessionId, cartVersion: 1, checkoutVersion: 1), showPanel: true, StorefrontFeatureDataMode.BrowserFetch, Actions);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.RefreshAsync());

            Assert.False(controller.State.Loading);
        }

        [Fact]
        public async Task PlaceOrderAsync_ExceptionResetsLoading()
        {
            var sessionId = Guid.NewGuid();
            var controller = CreateController(new FailingHandler());
            controller.Initialize(CreateState(sessionId, cartVersion: 1, checkoutVersion: 1), showPanel: true, StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.PlaceOrderAsync());

            Assert.False(controller.State.Loading);
        }

        private static readonly StorefrontCheckoutActionDescriptor Actions = new(
            "/api/checkout",
            "/api/checkout/shipping-method",
            "/api/checkout/payment-method",
            "/api/checkout/review",
            "/api/checkout/place-order");

        private static StorefrontBrowserCheckoutController CreateController(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_ => new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            });
            services.AddSingleton<IStorefrontAntiforgeryTokenReader>(new StaticTokenReader());
            services.AddSingleton<StorefrontLocalApiClient>();
            var provider = services.BuildServiceProvider();
            return new StorefrontBrowserCheckoutController(provider);
        }

        private static StorefrontBrowserCheckoutState CreateState(Guid sessionId, int cartVersion, int checkoutVersion)
        {
            return new StorefrontBrowserCheckoutState(
                HasCart: true,
                Message: null,
                sessionId,
                checkoutVersion,
                cartVersion,
                "active",
                "shipping",
                IsActive: true,
                ShippingRequired: true,
                PlaceOrderAllowed: true,
                "$25.00",
                Lines: [],
                ShippingOptions: [new StorefrontBrowserCheckoutOption("ground", "Ground", null, "$5.00", true)],
                PaymentMethods: [new StorefrontBrowserCheckoutOption("cod", "Cash", null, null, true)],
                Issues: []);
        }

        private static string ExtractIdempotencyKey(string requestBody)
        {
            using var document = JsonDocument.Parse(requestBody);
            return document.RootElement.GetProperty("idempotencyKey").GetString()
                ?? throw new InvalidOperationException("Place-order request did not contain idempotencyKey.");
        }

        private sealed class StaticTokenReader : IStorefrontAntiforgeryTokenReader
        {
            public ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<StorefrontAntiforgeryToken?>(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"));
            }
        }

        private sealed class QueueingHandler : HttpMessageHandler
        {
            private readonly Queue<(HttpContent Content, HttpStatusCode StatusCode)> _responses = new();

            public QueueingHandler(params object[] responses)
            {
                foreach (var response in responses)
                {
                    _responses.Enqueue((JsonContent(response), HttpStatusCode.OK));
                }
            }

            public QueueingHandler(object response)
                : this(JsonContent(response), HttpStatusCode.OK)
            {
            }

            public QueueingHandler(HttpContent content, HttpStatusCode statusCode)
            {
                _responses.Enqueue((content, statusCode));
            }

            public QueueingHandler(
                HttpContent firstContent,
                HttpStatusCode firstStatusCode,
                HttpContent secondContent,
                HttpStatusCode secondStatusCode)
            {
                _responses.Enqueue((firstContent, firstStatusCode));
                _responses.Enqueue((secondContent, secondStatusCode));
            }

            public List<HttpRequestMessage> Requests { get; } = [];

            public List<string> RequestBodies { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                RequestBodies.Add(request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken));
                var response = _responses.Dequeue();
                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = response.Content,
                    RequestMessage = request,
                };
            }

            private static StringContent JsonContent(object value)
            {
                var json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
        }

        private sealed class FailingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromException<HttpResponseMessage>(new InvalidOperationException("transport failed"));
            }
        }
    }
}
