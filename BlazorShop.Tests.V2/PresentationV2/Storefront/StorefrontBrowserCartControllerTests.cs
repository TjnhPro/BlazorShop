namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Browser;
    using BlazorShop.Storefront.Browser.Cart;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Components.Headless.Cart;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public sealed class StorefrontBrowserCartControllerTests
    {
        [Fact]
        public async Task HydrateAsync_LoadsCurrentCart_ForBrowserFetchWithoutSnapshot()
        {
            var lineId = Guid.NewGuid();
            var cart = CreateCart(lineId, count: 2);
            var handler = new QueueingHandler(cart);
            var publisher = new RecordingCartEventPublisher();
            var controller = CreateController(handler, publisher);
            controller.Initialize(null, [], StorefrontFeatureDataMode.BrowserFetch, Actions);

            var changed = await controller.HydrateAsync();

            Assert.True(changed);
            Assert.Equal(2, controller.State.Cart?.Count);
            Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/cart", handler.Requests.Single().RequestUri?.ToString());
            Assert.Equal([2], publisher.PublishedCounts);
        }

        [Fact]
        public async Task HydrateAsync_InitialSnapshotPublishesCountWithoutFetch()
        {
            var lineId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateCart(lineId, count: 9));
            var publisher = new RecordingCartEventPublisher();
            var controller = CreateController(handler, publisher);
            controller.Initialize(CreateCart(lineId, count: 3), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.HydrateAsync();

            Assert.True(changed);
            Assert.Empty(handler.Requests);
            Assert.Equal([3], publisher.PublishedCounts);
        }

        [Fact]
        public void Initialize_AcceptsNewerInitialSnapshot()
        {
            var firstLineId = Guid.NewGuid();
            var secondLineId = Guid.NewGuid();
            var controller = CreateController(new QueueingHandler(CreateCart(firstLineId)), new RecordingCartEventPublisher());

            controller.Initialize(CreateCart(firstLineId, count: 1, version: 1), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);
            controller.Initialize(CreateCart(secondLineId, count: 5, version: 2), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            Assert.Equal(5, controller.State.Cart?.Count);
            Assert.Equal(2, controller.State.Cart?.Version);
            Assert.Equal(secondLineId, controller.State.Lines.Single().LineId);
        }

        [Fact]
        public async Task Initialize_DoesNotOverwriteCurrentMutationWithOlderSnapshot()
        {
            var lineId = Guid.NewGuid();
            var handler = new BlockingHandler(CreateCart(lineId, count: 4, quantity: 4, version: 4));
            var controller = CreateController(handler, new RecordingCartEventPublisher());
            controller.Initialize(CreateCart(lineId, count: 2, quantity: 2, version: 3), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var updateTask = controller.UpdateQuantityAsync(lineId, "4");
            await handler.WaitForRequestAsync();
            controller.Initialize(CreateCart(Guid.NewGuid(), count: 1, version: 1), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            Assert.Equal(2, controller.State.Cart?.Count);
            Assert.Equal(lineId, controller.State.BusyLineId);

            handler.Release();
            Assert.True(await updateTask);
            Assert.Equal(4, controller.State.Cart?.Count);
            Assert.False(controller.State.BusyLineId.HasValue);
        }

        [Fact]
        public async Task UpdateQuantityAsync_RejectsBelowMinimumWithoutApiCall()
        {
            var lineId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateCart(lineId, count: 9));
            var controller = CreateController(handler, new RecordingCartEventPublisher());
            controller.Initialize(CreateCart(lineId, quantityMinimum: 2), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.UpdateQuantityAsync(lineId, "1");

            Assert.True(changed);
            Assert.Empty(handler.Requests);
            Assert.Contains("Minimum quantity", controller.State.Alerts.Single().Message, StringComparison.Ordinal);
            Assert.Equal("error", controller.State.Alerts.Single().Level);
        }

        [Fact]
        public async Task UpdateQuantityAsync_SendsQuantityRequestAndPublishesCount()
        {
            var lineId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateCart(lineId, count: 5, quantity: 5));
            var publisher = new RecordingCartEventPublisher();
            var controller = CreateController(handler, publisher);
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.UpdateQuantityAsync(lineId, "5");

            Assert.True(changed);
            var request = handler.Requests.Single();
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal($"https://storefront.example/api/cart/lines/{lineId:D}", request.RequestUri?.ToString());
            Assert.Contains("\"quantity\":5", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Equal(5, controller.State.Cart?.Count);
            Assert.Equal([5], publisher.PublishedCounts);
        }

        [Fact]
        public async Task RemoveLineAsync_DeletesLineRouteAndAppliesResult()
        {
            var lineId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateCart(lineId, count: 0, lines: []));
            var publisher = new RecordingCartEventPublisher();
            var controller = CreateController(handler, publisher);
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.RemoveLineAsync(lineId);

            Assert.True(changed);
            Assert.Equal(HttpMethod.Delete, handler.Requests.Single().Method);
            Assert.Equal($"https://storefront.example/api/cart/lines/{lineId:D}", handler.Requests.Single().RequestUri?.ToString());
            Assert.Empty(controller.State.Lines);
            Assert.Equal([0], publisher.PublishedCounts);
        }

        [Fact]
        public async Task RemoveLineAndClear_RespectBusyMutationState()
        {
            var lineId = Guid.NewGuid();
            var handler = new BlockingHandler(CreateCart(lineId, count: 4, quantity: 4));
            var controller = CreateController(handler, new RecordingCartEventPublisher());
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var updateTask = controller.UpdateQuantityAsync(lineId, "4");
            await handler.WaitForRequestAsync();

            var removeChanged = await controller.RemoveLineAsync(lineId);
            var clearChanged = await controller.ClearAsync();
            handler.Release();
            var updateChanged = await updateTask;

            Assert.True(updateChanged);
            Assert.False(removeChanged);
            Assert.False(clearChanged);
            Assert.Single(handler.Requests);
            Assert.False(controller.State.BusyLineId.HasValue);
            Assert.False(controller.State.Clearing);
        }

        [Fact]
        public async Task ClearAsync_DeletesClearRouteAndAppliesResult()
        {
            var lineId = Guid.NewGuid();
            var handler = new QueueingHandler(CreateCart(lineId, count: 0, lines: []));
            var publisher = new RecordingCartEventPublisher();
            var controller = CreateController(handler, publisher);
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            var changed = await controller.ClearAsync();

            Assert.True(changed);
            Assert.Equal(HttpMethod.Delete, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/cart", handler.Requests.Single().RequestUri?.ToString());
            Assert.Empty(controller.State.Lines);
            Assert.Equal([0], publisher.PublishedCounts);
        }

        [Fact]
        public async Task LoadAsync_MapsApiErrorToCartAlert()
        {
            var error = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse(
                    "Cart changed. Refresh and try again.",
                    "cart.conflict",
                    "trace-cart",
                    null,
                    false,
                    409),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new QueueingHandler(new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.Conflict);
            var controller = CreateController(handler, new RecordingCartEventPublisher());
            controller.Initialize(null, [], StorefrontFeatureDataMode.BrowserFetch, Actions);

            var changed = await controller.LoadAsync();

            Assert.True(changed);
            Assert.Null(controller.State.Cart);
            Assert.Equal("error", controller.State.Alerts.Single().Level);
            Assert.Equal("Cart changed. Refresh and try again.", controller.State.Alerts.Single().Message);
        }

        [Fact]
        public async Task UpdateQuantityAsync_ExceptionResetsBusyLine()
        {
            var lineId = Guid.NewGuid();
            var controller = CreateController(new FailingHandler(), new RecordingCartEventPublisher());
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.UpdateQuantityAsync(lineId, "2"));

            Assert.False(controller.State.BusyLineId.HasValue);
        }

        [Fact]
        public async Task RemoveLineAsync_ExceptionResetsBusyLine()
        {
            var lineId = Guid.NewGuid();
            var controller = CreateController(new FailingHandler(), new RecordingCartEventPublisher());
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.RemoveLineAsync(lineId));

            Assert.False(controller.State.BusyLineId.HasValue);
        }

        [Fact]
        public async Task ClearAsync_ExceptionResetsClearingState()
        {
            var lineId = Guid.NewGuid();
            var controller = CreateController(new FailingHandler(), new RecordingCartEventPublisher());
            controller.Initialize(CreateCart(lineId), [], StorefrontFeatureDataMode.InitialSnapshot, Actions);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ClearAsync());

            Assert.False(controller.State.Clearing);
        }

        private static readonly StorefrontCartActionDescriptor Actions = new(
            "/api/cart",
            "/api/cart/lines/{lineId}",
            "/api/cart/lines/{lineId}",
            "/api/cart");

        private static StorefrontBrowserCartController CreateController(
            HttpMessageHandler handler,
            RecordingCartEventPublisher publisher)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_ => new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            });
            services.AddSingleton<IStorefrontAntiforgeryTokenReader>(new StaticTokenReader());
            services.AddSingleton<StorefrontLocalApiClient>();
            services.AddSingleton<IStorefrontBrowserCartEventPublisher>(publisher);
            var provider = services.BuildServiceProvider();
            return new StorefrontBrowserCartController(provider);
        }

        private static StorefrontBrowserCart CreateCart(
            Guid lineId,
            int count = 1,
            int version = 1,
            int quantity = 1,
            int quantityMinimum = 1,
            IReadOnlyList<StorefrontBrowserCartLine>? lines = null)
        {
            var cartLines = lines ?? [CreateLine(lineId, quantity, quantityMinimum)];
            return new StorefrontBrowserCart(
                count,
                version,
                cartLines,
                "USD",
                Subtotal: 10,
                "$10.00",
                GrandTotal: 10,
                "$10.00",
                CheckoutAllowed: true,
                Warnings: [],
                Adjustments: []);
        }

        private static StorefrontBrowserCartLine CreateLine(Guid lineId, int quantity, int quantityMinimum)
        {
            return new StorefrontBrowserCartLine(
                lineId,
                ProductId: Guid.NewGuid(),
                ProductVariantId: null,
                "Canvas Tote",
                "/products/canvas-tote",
                ImageUrl: null,
                quantity,
                UnitPrice: 10,
                "$10.00",
                LineTotal: 10 * quantity,
                "$10.00",
                "USD",
                VariantLabel: null,
                quantityMinimum,
                QuantityMaximum: null,
                QuantityStep: 1,
                Warnings: [],
                IsUnavailable: false);
        }

        private sealed class StaticTokenReader : IStorefrontAntiforgeryTokenReader
        {
            public ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<StorefrontAntiforgeryToken?>(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"));
            }
        }

        private sealed class RecordingCartEventPublisher : IStorefrontBrowserCartEventPublisher
        {
            private readonly List<int> _publishedCounts = [];

            public IReadOnlyList<int> PublishedCounts => _publishedCounts;

            public ValueTask PublishCartChangedAsync(int count, CancellationToken cancellationToken = default)
            {
                _publishedCounts.Add(count);
                return ValueTask.CompletedTask;
            }
        }

        private sealed class QueueingHandler : HttpMessageHandler
        {
            private readonly Queue<(HttpContent Content, HttpStatusCode StatusCode)> _responses = new();

            public QueueingHandler(StorefrontBrowserCart cart)
                : this(JsonContent(cart), HttpStatusCode.OK)
            {
            }

            public QueueingHandler(HttpContent content, HttpStatusCode statusCode)
            {
                _responses.Enqueue((content, statusCode));
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

            private static StringContent JsonContent(StorefrontBrowserCart cart)
            {
                var json = JsonSerializer.Serialize(cart, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
        }

        private sealed class BlockingHandler : HttpMessageHandler
        {
            private readonly StorefrontBrowserCart _cart;
            private readonly TaskCompletionSource _requestReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public BlockingHandler(StorefrontBrowserCart cart)
            {
                _cart = cart;
            }

            public List<HttpRequestMessage> Requests { get; } = [];

            public Task WaitForRequestAsync()
            {
                return _requestReceived.Task;
            }

            public void Release()
            {
                _release.SetResult();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                _requestReceived.SetResult();
                await _release.Task.WaitAsync(cancellationToken);
                var json = JsonSerializer.Serialize(_cart, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    RequestMessage = request,
                };
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
