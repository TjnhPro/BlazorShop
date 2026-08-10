namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Net;
using System.Text;
using System.Text.Json;

using BlazorShop.Storefront.Browser;
using BlazorShop.Storefront.Browser.Contact;
using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Contracts.Contact;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class StorefrontBrowserContactControllerTests
{
    [Fact]
    public void AddStorefrontBrowserControllers_RegistersContactController()
    {
        var services = new ServiceCollection();
        services.AddStorefrontBrowserControllers();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<StorefrontBrowserContactController>(provider.GetRequiredService<IStorefrontBrowserContactController>());
    }

    [Fact]
    public async Task SubmitAsync_PostsToDefaultSameOriginRouteWithAntiforgery()
    {
        var response = new StorefrontContactFormSubmitResult(true, DefaultMessage: "Accepted.");
        var handler = new QueueingHandler(response);
        var controller = CreateController(handler);

        var result = await controller.SubmitAsync(CreateRequest());

        Assert.True(result.Success);
        Assert.Equal("Accepted.", result.DefaultMessage);
        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("https://storefront.example/api/contact", handler.Requests.Single().RequestUri?.ToString());
        Assert.Contains("\"subject\":\"Question\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
        Assert.True(handler.Requests.Single().Headers.Contains("X-CSRF-TOKEN"));
    }

    [Fact]
    public async Task SubmitAsync_PostsToDescriptorRoute()
    {
        var handler = new QueueingHandler(new StorefrontContactFormSubmitResult(true));
        var controller = CreateController(handler);

        await controller.SubmitAsync(
            CreateRequest(),
            new StorefrontContactFormActionDescriptor("/api/contact/custom"));

        Assert.Equal("https://storefront.example/api/contact/custom", handler.Requests.Single().RequestUri?.ToString());
    }

    [Theory]
    [InlineData("https://commerce.example/api/contact")]
    [InlineData("//commerce.example/api/contact")]
    public async Task SubmitAsync_RejectsAbsoluteOrProtocolRelativeRoutes(string route)
    {
        var handler = new QueueingHandler(new StorefrontContactFormSubmitResult(true));
        var controller = CreateController(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => controller.SubmitAsync(
            CreateRequest(),
            new StorefrontContactFormActionDescriptor(route)));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SubmitAsync_MapsValidationErrorResponse()
    {
        var error = JsonSerializer.Serialize(
            new StorefrontLocalApiErrorResponse(
                "The contact request is invalid.",
                "validation_error",
                "trace-contact",
                new Dictionary<string, string[]> { ["Email"] = ["Enter a valid email address."] },
                false,
                400),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var handler = new QueueingHandler(new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.BadRequest);
        var controller = CreateController(handler);

        var result = await controller.SubmitAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Equal("validation_error", result.Code);
        Assert.Equal("The contact request is invalid.", result.DefaultMessage);
        Assert.Equal("trace-contact", result.TraceId);
        Assert.False(result.Retryable);
        Assert.Equal("Enter a valid email address.", result.FieldErrors!["Email"].Single());
    }

    [Fact]
    public async Task SubmitAsync_PropagatesCancellation()
    {
        var handler = new BlockingHandler();
        var controller = CreateController(handler);
        using var cts = new CancellationTokenSource();

        var submitTask = controller.SubmitAsync(CreateRequest(), cancellationToken: cts.Token);
        await handler.WaitForRequestAsync();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => submitTask);
    }

    private static StorefrontBrowserContactController CreateController(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://storefront.example/"),
        });
        services.AddSingleton<IStorefrontAntiforgeryTokenReader>(new StaticAntiforgeryTokenReader());
        services.AddScoped<StorefrontLocalApiClient>();

        return new StorefrontBrowserContactController(services.BuildServiceProvider());
    }

    private static StorefrontContactFormSubmitRequest CreateRequest()
    {
        return new StorefrontContactFormSubmitRequest(
            "Taylor",
            "taylor@example.test",
            "Question",
            "Can you help?");
    }

    private sealed class QueueingHandler : HttpMessageHandler
    {
        private readonly HttpContent _content;
        private readonly HttpStatusCode _statusCode;

        public QueueingHandler(object response)
            : this(JsonContent(response), HttpStatusCode.OK)
        {
        }

        public QueueingHandler(HttpContent content, HttpStatusCode statusCode)
        {
            _content = content;
            _statusCode = statusCode;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(_statusCode) { Content = _content };
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
        private readonly TaskCompletionSource _requestSeen = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<HttpRequestMessage> Requests { get; } = [];

        public Task WaitForRequestAsync() => _requestSeen.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            _requestSeen.TrySetResult();
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
