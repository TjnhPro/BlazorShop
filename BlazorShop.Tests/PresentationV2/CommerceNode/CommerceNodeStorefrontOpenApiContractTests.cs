extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;

    using Xunit;

    using CommerceNodeProgram = CommerceNodeApi::Program;

    public sealed class CommerceNodeStorefrontOpenApiContractTests : IClassFixture<WebApplicationFactory<CommerceNodeProgram>>
    {
        private const string StorefrontSwaggerPath = "/swagger/storefront/swagger.json";
        private const string SnapshotPath = "PresentationV2/CommerceNode/Snapshots/storefront-openapi.paths.snapshot.txt";

        private readonly WebApplicationFactory<CommerceNodeProgram> factory;

        public CommerceNodeStorefrontOpenApiContractTests(WebApplicationFactory<CommerceNodeProgram> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task StorefrontSwagger_CanBeFetchedAndParsed()
        {
            using var client = this.CreateSwaggerClient();

            using var response = await client.GetAsync(StorefrontSwaggerPath);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            Assert.Equal("3.0.4", root.GetProperty("openapi").GetString());
            Assert.Equal("Storefront API", root.GetProperty("info").GetProperty("title").GetString());
            Assert.True(root.GetProperty("paths").EnumerateObject().Any());
        }

        [Fact]
        public async Task StorefrontSwagger_PathSnapshotMatchesBaseline()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var actual = GetPathSnapshot(swagger);
            var expected = await File.ReadAllTextAsync(GetSnapshotAbsolutePath());

            Assert.Equal(NormalizeLineEndings(expected), actual);
        }

        private HttpClient CreateSwaggerClient()
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private async Task<JsonObject> GetStorefrontSwaggerAsync()
        {
            using var client = this.CreateSwaggerClient();
            var content = await client.GetStringAsync(StorefrontSwaggerPath);

            return JsonNode.Parse(content)?.AsObject()
                ?? throw new InvalidOperationException("Storefront Swagger response was not a JSON object.");
        }

        private static string GetPathSnapshot(JsonObject swagger)
        {
            var paths = swagger["paths"]?.AsObject()
                ?? throw new InvalidOperationException("Storefront Swagger document does not contain paths.");

            var lines = paths
                .OrderBy(path => path.Key, StringComparer.Ordinal)
                .SelectMany(path => path.Value?.AsObject()
                    .Where(operation => IsHttpMethod(operation.Key))
                    .OrderBy(operation => operation.Key, StringComparer.Ordinal)
                    .Select(operation => $"{operation.Key.ToUpperInvariant()} {path.Key}")
                    ?? Enumerable.Empty<string>());

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static bool IsHttpMethod(string value)
        {
            return value.Equals("get", StringComparison.OrdinalIgnoreCase)
                || value.Equals("post", StringComparison.OrdinalIgnoreCase)
                || value.Equals("put", StringComparison.OrdinalIgnoreCase)
                || value.Equals("patch", StringComparison.OrdinalIgnoreCase)
                || value.Equals("delete", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSnapshotAbsolutePath()
        {
            return Path.Combine(AppContext.BaseDirectory, SnapshotPath);
        }

        private static string NormalizeLineEndings(string value)
        {
            return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", Environment.NewLine, StringComparison.Ordinal);
        }
    }
}
