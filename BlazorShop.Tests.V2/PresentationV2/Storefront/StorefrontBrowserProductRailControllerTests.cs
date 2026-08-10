namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Net;
using System.Text;
using System.Text.Json;

using BlazorShop.Storefront.Browser;
using BlazorShop.Storefront.Browser.Catalog;
using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Contracts.Catalog;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class StorefrontBrowserProductRailControllerTests
{
    [Fact]
    public void AddStorefrontBrowserControllers_RegistersProductRailController()
    {
        var services = new ServiceCollection();
        services.AddStorefrontBrowserControllers();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<StorefrontBrowserProductRailController>(
            provider.GetRequiredService<IStorefrontBrowserProductRailController>());
    }

    [Fact]
    public async Task GetDiscountedProductRailAsync_LoadsDefaultSameOriginRouteWithLimit()
    {
        var response = new StorefrontDiscountedProductRailResponse([Product("discounted")]);
        var handler = new QueueingHandler(response);
        var controller = CreateController(handler);

        var result = await controller.GetDiscountedProductRailAsync(6);

        var product = Assert.Single(result.Products);
        Assert.True(result.Success);
        Assert.Equal("Discounted", product.Name);
        Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
        Assert.Equal(
            "https://storefront.example/api/catalog/discounted-products?limit=6",
            handler.Requests.Single().RequestUri?.ToString());
        Assert.False(handler.Requests.Single().Headers.Contains("X-CSRF-TOKEN"));
    }

    [Fact]
    public async Task GetDiscountedProductRailAsync_LoadsDescriptorRoute()
    {
        var handler = new QueueingHandler(new StorefrontDiscountedProductRailResponse([]));
        var controller = CreateController(handler);

        await controller.GetDiscountedProductRailAsync(
            4,
            new StorefrontDiscountedProductRailActionDescriptor("local/discounted?source=home"));

        Assert.Equal(
            "https://storefront.example/local/discounted?source=home&limit=4",
            handler.Requests.Single().RequestUri?.ToString());
    }

    [Theory]
    [InlineData("https://commerce.example/api/catalog/discounted-products")]
    [InlineData("//commerce.example/api/catalog/discounted-products")]
    public async Task GetDiscountedProductRailAsync_RejectsAbsoluteOrProtocolRelativeRoutes(string route)
    {
        var handler = new QueueingHandler(new StorefrontDiscountedProductRailResponse([]));
        var controller = CreateController(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => controller.GetDiscountedProductRailAsync(
            6,
            new StorefrontDiscountedProductRailActionDescriptor(route)));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetDiscountedProductRailAsync_MapsEmptySuccess()
    {
        var handler = new QueueingHandler(new StorefrontDiscountedProductRailResponse([]));
        var controller = CreateController(handler);

        var result = await controller.GetDiscountedProductRailAsync(6);

        Assert.True(result.Success);
        Assert.Empty(result.Products);
        Assert.Null(result.Code);
    }

    [Fact]
    public async Task GetDiscountedProductRailAsync_MapsErrorResponse()
    {
        var error = JsonSerializer.Serialize(
            new StorefrontLocalApiErrorResponse(
                "Catalog data is temporarily unavailable.",
                "service_unavailable",
                "trace-rail",
                [],
                true,
                503),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var handler = new QueueingHandler(
            new StringContent(error, Encoding.UTF8, "application/json"),
            HttpStatusCode.ServiceUnavailable);
        var controller = CreateController(handler);

        var result = await controller.GetDiscountedProductRailAsync(6);

        Assert.False(result.Success);
        Assert.Empty(result.Products);
        Assert.Equal("service_unavailable", result.Code);
        Assert.Equal("Catalog data is temporarily unavailable.", result.DefaultMessage);
        Assert.Equal("trace-rail", result.TraceId);
        Assert.True(result.Retryable);
    }

    [Fact]
    public async Task GetDiscountedProductRailAsync_PropagatesCancellation()
    {
        var handler = new BlockingHandler();
        var controller = CreateController(handler);
        using var cts = new CancellationTokenSource();

        var loadTask = controller.GetDiscountedProductRailAsync(6, cancellationToken: cts.Token);
        await handler.WaitForRequestAsync();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => loadTask);
    }

    private static StorefrontBrowserProductRailController CreateController(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://storefront.example/"),
        });
        services.AddSingleton<IStorefrontAntiforgeryTokenReader>(new StaticAntiforgeryTokenReader());
        services.AddScoped<StorefrontLocalApiClient>();

        return new StorefrontBrowserProductRailController(services.BuildServiceProvider());
    }

    private static ProductSummaryItem Product(string slug)
    {
        return new ProductSummaryItem(
            Guid.NewGuid(),
            slug.Replace('-', ' ').ToTitleCaseInvariant(),
            "/" + slug,
            "Category",
            "/category",
            ImageUrl: null,
            Description: null,
            "USD 10.00",
            "USD 12.00",
            HasVariants: false,
            InStock: true,
            IsNewArrival: false,
            Purchasable: true,
            PurchaseUrl: "/" + slug + "#purchase");
    }

    private sealed class QueueingHandler : HttpMessageHandler
    {
        private readonly HttpContent content;
        private readonly HttpStatusCode statusCode;

        public QueueingHandler(object response)
            : this(JsonContent(response), HttpStatusCode.OK)
        {
        }

        public QueueingHandler(HttpContent content, HttpStatusCode statusCode)
        {
            this.content = content;
            this.statusCode = statusCode;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(this.statusCode) { Content = this.content });
        }

        private static StringContent JsonContent(object response)
        {
            return new StringContent(
                JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                Encoding.UTF8,
                "application/json");
        }
    }

    private sealed class BlockingHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource requestSeen = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<HttpRequestMessage> Requests { get; } = [];

        public Task WaitForRequestAsync() => this.requestSeen.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Requests.Add(request);
            this.requestSeen.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class StaticAntiforgeryTokenReader : IStorefrontAntiforgeryTokenReader
    {
        public ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<StorefrontAntiforgeryToken?>(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"));
        }
    }
}
