namespace BlazorShop.Tests.Architecture
{
    using System.Net;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Xunit;

    public sealed class ServiceDefaultsHttpClientResilienceTests
    {
        [Fact]
        public async Task AddServiceDefaults_WhenHttpClientResilienceDisabled_DoesNotRetryTransientFailures()
        {
            var handler = new CountingServiceUnavailableHandler();
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceDefaults:HttpClientResilience:Enabled"] = "false",
            });

            builder.AddServiceDefaults();
            builder.Services
                .AddHttpClient("probe")
                .ConfigurePrimaryHttpMessageHandler(() => handler);

            using var serviceProvider = builder.Services.BuildServiceProvider();
            var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("probe");

            using var response = await client.GetAsync("https://commerce-node.example/api/storefront/stores/default/configuration");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(1, handler.AttemptCount);
        }

        [Fact]
        public void AddServiceDefaults_KeepsHttpClientResilienceEnabledByDefault()
        {
            var source = ReadRepositoryFile("BlazorShop.ServiceDefaults/Extensions.cs");

            Assert.Contains(
                "GetValue(\"ServiceDefaults:HttpClientResilience:Enabled\", true)",
                source,
                StringComparison.Ordinal);
            Assert.Contains("if (httpClientResilienceEnabled)", source, StringComparison.Ordinal);
            Assert.Contains("http.AddStandardResilienceHandler();", source, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        private sealed class CountingServiceUnavailableHandler : HttpMessageHandler
        {
            public int AttemptCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                AttemptCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
        }
    }
}
