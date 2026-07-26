namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net.Http;

    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;

    using Xunit;

    public sealed class StorefrontRuntimeResultPrimitiveTests
    {
        [Theory]
        [InlineData(401, "auth.required")]
        [InlineData(403, "auth.forbidden")]
        [InlineData(404, "catalog.not_found")]
        [InlineData(409, "cart.version_conflict")]
        [InlineData(422, "validation.failed")]
        [InlineData(503, "storefront.unavailable")]
        public void RuntimeErrorMapper_MapsTypedApiErrorStatus(int status, string code)
        {
            var exception = CreateApiException(status, code, "Mapped message.");

            var error = StorefrontRuntimeErrorMapper.FromApiException(exception);

            Assert.Equal(status, error.Status);
            Assert.Equal(code, error.Code);
            Assert.Equal("Mapped message.", error.Message);
            Assert.Equal("trace-1", error.TraceId);
        }

        [Fact]
        public void RuntimeErrorMapper_MapsTimeoutExceptionToServiceUnavailable()
        {
            var error = StorefrontRuntimeErrorMapper.FromException(new TimeoutException("Timed out."));

            Assert.Equal(StorefrontRuntimeStatusCodes.ServiceUnavailable, error.Status);
            Assert.Equal("network.timeout", error.Code);
            Assert.Contains("timed out", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact(Skip = "Enable in SRH5 after Runtime error primitives expose Retryable.")]
        public void RuntimeErrorMapper_TimeoutIsRetryable()
        {
            var retryableProperty = typeof(StorefrontRuntimeError).GetProperty("Retryable");

            Assert.NotNull(retryableProperty);
        }

        [Fact]
        public void RuntimeErrorMapper_MapsNetworkExceptionToServiceUnavailable()
        {
            var error = StorefrontRuntimeErrorMapper.FromException(new HttpRequestException("Connection failed."));

            Assert.Equal(StorefrontRuntimeStatusCodes.ServiceUnavailable, error.Status);
            Assert.Equal("network.failure", error.Code);
        }

        [Fact]
        public void RuntimeErrorMapper_PreservesValidationPayload()
        {
            var exception = CreateApiException(
                StorefrontRuntimeStatusCodes.Validation,
                "validation.failed",
                "Validation failed.",
                new Dictionary<string, ICollection<string>>(StringComparer.Ordinal)
                {
                    ["quantity"] = ["Quantity must be at least 1."],
                });

            var error = StorefrontRuntimeErrorMapper.FromApiException(exception);

            Assert.Single(error.FieldErrors);
            Assert.Equal(["Quantity must be at least 1."], error.FieldErrors["quantity"]);
            var validation = Assert.Single(error.ValidationErrors);
            Assert.Equal("quantity", validation.Field);
            Assert.Equal(["Quantity must be at least 1."], validation.Messages);
        }

        [Fact]
        public void RuntimeErrorMapper_ExposesConflictPrimitive()
        {
            var exception = CreateApiException(
                StorefrontRuntimeStatusCodes.Conflict,
                "cart.version_conflict",
                "Cart version mismatch.");

            var error = StorefrontRuntimeErrorMapper.FromApiException(exception);

            Assert.NotNull(error.Conflict);
            Assert.Equal("cart.version_conflict", error.Conflict!.Code);
            Assert.Equal("Cart version mismatch.", error.Conflict.Message);
        }

        [Fact]
        public async Task ExecuteAsync_RequiresExplicitStoreKeyAndPassesItToCall()
        {
            var context = new StubRuntimeContext(" default ");

            var result = await context.ExecuteAsync((storeKey, _) => Task.FromResult(storeKey));

            Assert.True(result.Success);
            Assert.Equal("default", result.Value);
        }

        [Fact]
        public async Task ExecuteSubmitAsync_MapsApiExceptionToSubmitResult()
        {
            var context = new StubRuntimeContext("default");

            var result = await context.ExecuteSubmitAsync<string>(
                (_, _) => throw CreateApiException(StorefrontRuntimeStatusCodes.Conflict, "cart.version_conflict", "Cart version mismatch."),
                "idem-1");

            Assert.False(result.Success);
            Assert.Equal("idem-1", result.IdempotencyKey);
            Assert.Equal(StorefrontRuntimeStatusCodes.Conflict, result.Error!.Status);
        }

        [Fact(Skip = "Enable in SRH3 after caller cancellation is distinguished from timeout.")]
        public async Task ExecuteAsync_CallerRequestedTaskCancellationPropagates()
        {
            var context = new StubRuntimeContext("default");
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                context.ExecuteAsync<string>(
                    (_, token) => Task.FromCanceled<string>(token),
                    cancellation.Token));
        }

        [Fact]
        public async Task ExecuteAsync_NonCallerTaskCancellationMapsToTimeout()
        {
            var context = new StubRuntimeContext("default");

            var result = await context.ExecuteAsync<string>(
                (_, _) => Task.FromCanceled<string>(new CancellationToken(canceled: true)),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(StorefrontRuntimeStatusCodes.ServiceUnavailable, result.Error!.Status);
            Assert.Equal("network.timeout", result.Error.Code);
        }

        [Fact]
        public void RequireStoreKey_RejectsBlankStoreKey()
        {
            var context = new StubRuntimeContext(" ");

            var exception = Assert.Throws<InvalidOperationException>(() => context.RequireStoreKey());

            Assert.Contains("storeKey", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RuntimePackage_DoesNotReferenceRazorComponentsOrV2Host()
        {
            var project = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj"));

            Assert.DoesNotContain("Microsoft.NET.Sdk.Razor", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", project, StringComparison.Ordinal);
        }

        private static StorefrontApiException<CommerceNodeApiErrorResponse> CreateApiException(
            int status,
            string code,
            string message,
            IDictionary<string, ICollection<string>>? fieldErrors = null)
        {
            return new StorefrontApiException<CommerceNodeApiErrorResponse>(
                message,
                status,
                "{}",
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                new CommerceNodeApiErrorResponse
                {
                    Success = false,
                    Code = code,
                    Message = message,
                    TraceId = "trace-1",
                    FieldErrors = fieldErrors,
                },
                null);
        }

        private static string RepositoryPath(string relativePath)
        {
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "BlazorShop.sln")))
            {
                root = Directory.GetParent(root)?.FullName
                    ?? throw new InvalidOperationException("Could not locate repository root.");
            }

            return Path.GetFullPath(Path.Combine(root, relativePath));
        }

        private sealed class StubRuntimeContext : IStorefrontRuntimeContext
        {
            public StubRuntimeContext(string storeKey)
            {
                this.StoreKey = storeKey;
            }

            public string CommerceNodeBaseUrl => "http://localhost:5180";

            public string StoreKey { get; }

            public string? PublicBaseUrl => "http://localhost:18598";
        }
    }
}
