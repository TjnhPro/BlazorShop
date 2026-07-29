namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Browser;
    using BlazorShop.Storefront.Browser.Account;
    using BlazorShop.Storefront.Browser.Cart;
    using BlazorShop.Storefront.Browser.Checkout;
    using BlazorShop.Storefront.Components.Browser;
    using Microsoft.Extensions.DependencyInjection;

    using Xunit;

    public sealed class StorefrontBrowserRuntimeFoundationTests
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
        public async Task LocalApiClient_HttpRequestExceptionReturnsRetryableNetworkError()
        {
            using var httpClient = new HttpClient(new ThrowingHandler(new HttpRequestException("offline")))
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            var result = await client.GetAsync<MutationResult>("/api/cart");

            Assert.False(result.Success);
            Assert.Equal("network_error", result.Error?.Code);
            Assert.True(result.Error?.Retryable);
        }

        [Fact]
        public async Task LocalApiClient_TimeoutNotCausedByCallerCancellationReturnsRetryableTimeout()
        {
            using var httpClient = new HttpClient(new ThrowingHandler(new TaskCanceledException("timeout")))
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            var result = await client.GetAsync<MutationResult>("/api/cart");

            Assert.False(result.Success);
            Assert.Equal("timeout", result.Error?.Code);
            Assert.True(result.Error?.Retryable);
        }

        [Fact]
        public async Task LocalApiClient_CallerCancellationPropagatesOperationCanceledException()
        {
            using var httpClient = new HttpClient(new ThrowingHandler(new TaskCanceledException("cancelled")))
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync<MutationResult>("/api/cart", cancellation.Token));
        }

        [Fact]
        public async Task LocalApiClient_MalformedSuccessfulJsonReturnsInvalidResponse()
        {
            var handler = new RecordingHandler(
                HttpStatusCode.OK,
                new StringContent("{ malformed json", Encoding.UTF8, "application/json"));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            };
            var client = new StorefrontLocalApiClient(
                httpClient,
                new StubAntiforgeryTokenReader(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token")));

            var result = await client.GetAsync<MutationResult>("/api/cart");

            Assert.False(result.Success);
            Assert.Equal("invalid_response", result.Error?.Code);
            Assert.True(result.Error?.Retryable);
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
        public void BrowserProject_IsBrowserSafeAndOwnsLocalApiRuntimePrimitives()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj");
            var source = ReadSourceTree("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser");
            var componentsSource = ReadSourceTree("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser");

            Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", project, StringComparison.Ordinal);
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", project, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.Components.csproj", project, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalApiClient", source, StringComparison.Ordinal);
            Assert.Contains("IStorefrontAntiforgeryTokenReader", source, StringComparison.Ordinal);
            Assert.Contains("StorefrontBrowserCartEventPublisher", source, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontBrowserRuntime", source, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalApiClient", componentsSource, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontAntiforgeryTokenReader", componentsSource, StringComparison.Ordinal);

            foreach (var forbiddenReference in new[]
            {
                "BlazorShop.Storefront.Presentation",
                "BlazorShop.Storefront.Runtime",
                "BlazorShop.Storefront.Client",
                "BlazorShop.Storefront.V2",
                "BlazorShop.Storefront.V2.WASM",
                "BlazorShop.ServiceDefaults",
                "BlazorShop.Application",
                "BlazorShop.Domain",
                "BlazorShop.Infrastructure",
                "BlazorShop.CommerceNode.API",
                "BlazorShop.ControlPlane"
            })
            {
                Assert.DoesNotContain(forbiddenReference, project, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void BrowserControllerRegistrations_AreTransientWhileRuntimeInfrastructureStaysScoped()
        {
            var services = new ServiceCollection();

            services.AddScoped<IStorefrontAntiforgeryTokenReader, StubAntiforgeryTokenReader>();
            services.AddScoped<StorefrontLocalApiClient>();
            services.AddScoped<IStorefrontBrowserCartEventPublisher, StubCartEventPublisher>();
            services.AddStorefrontBrowserCart();
            services.AddStorefrontBrowserCheckout();
            services.AddStorefrontBrowserAccount();

            Assert.Equal(ServiceLifetime.Transient, FindLifetime<IStorefrontBrowserCartController>(services));
            Assert.Equal(ServiceLifetime.Transient, FindLifetime<IStorefrontBrowserCheckoutController>(services));
            Assert.Equal(ServiceLifetime.Transient, FindLifetime<IStorefrontBrowserAccountController>(services));
            Assert.Equal(ServiceLifetime.Scoped, FindLifetime<IStorefrontAntiforgeryTokenReader>(services));
            Assert.Equal(ServiceLifetime.Scoped, FindLifetime<StorefrontLocalApiClient>(services));
            Assert.Equal(ServiceLifetime.Scoped, FindLifetime<IStorefrontBrowserCartEventPublisher>(services));
        }

        private static string ReadSourceTree(string relativeRoot)
        {
            var root = ResolveRepositoryPath(relativeRoot);
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
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

        private sealed record CartSummary(int Count);

        private sealed record MutationResult(bool Success);

        private sealed class StubAntiforgeryTokenReader : IStorefrontAntiforgeryTokenReader
        {
            public StubAntiforgeryTokenReader()
                : this(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"))
            {
            }

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

        private sealed class StubCartEventPublisher : IStorefrontBrowserCartEventPublisher
        {
            public ValueTask PublishCartChangedAsync(int count, CancellationToken cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }
        }

        private static ServiceLifetime FindLifetime<TService>(IServiceCollection services)
        {
            return services.Last(descriptor => descriptor.ServiceType == typeof(TService)).Lifetime;
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

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            private readonly Exception _exception;

            public ThrowingHandler(Exception exception)
            {
                _exception = exception;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromException<HttpResponseMessage>(_exception);
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
