extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Text;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using Xunit;

    using CommerceNodeProgram = CommerceNodeApi::Program;

    public sealed class CommerceNodeStorefrontPaymentContractTests : IClassFixture<WebApplicationFactory<CommerceNodeProgram>>
    {
        private readonly WebApplicationFactory<CommerceNodeProgram> factory;

        public CommerceNodeStorefrontPaymentContractTests(WebApplicationFactory<CommerceNodeProgram> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task PayPalCompatibilityCaptureRoute_IsRetired()
        {
            using var client = this.CreateClient();
            using var body = new StringContent("{\"token\":\"demo-token\"}", Encoding.UTF8, "application/json");

            using var post = await client.PostAsync(
                "/api/storefront/stores/test-store/payments/paypal/capture",
                body);
            using var get = await client.GetAsync(
                "/api/storefront/stores/test-store/payments/paypal/capture?token=demo-token");

            Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public void StorefrontPaymentEndpoints_DoNotExposeProviderDescriptorsOrSettingsJson()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedPaymentsController.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedConfigurationController.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/PaymentContracts.cs")
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/ConfigurationContracts.cs");

            Assert.DoesNotContain("PaymentProviderDescriptor", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SettingsJson", source, StringComparison.Ordinal);
        }

        private HttpClient CreateClient()
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ICommerceStoreDomainResolver>();
                    services.AddScoped<ICommerceStoreDomainResolver, StubCommerceStoreDomainResolver>();
                });
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class StubCommerceStoreDomainResolver : ICommerceStoreDomainResolver
        {
            private static readonly Guid StoreId = Guid.NewGuid();

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(CreateCurrentStore()));
            }

            public Task<ApplicationResult<StoreExecutionContext>> ResolveExecutionContextAsync(
                string? storeKey = null,
                string? host = null,
                string source = StoreExecutionContextSources.Unknown,
                CancellationToken cancellationToken = default)
            {
                var currentStore = CreateCurrentStore();
                return Task.FromResult(ApplicationResult<StoreExecutionContext>.Succeeded(
                    new StoreExecutionContext(
                        StoreId,
                        currentStore.StoreKey,
                        host,
                        source,
                        currentStore.Status,
                        true,
                        currentStore)));
            }

            public Task<ApplicationResult<Guid>> ResolveStoreIdAsync(
                string? storeKey = null,
                string? host = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ApplicationResult<Guid>.Succeeded(StoreId));
            }

            private static CommerceCurrentStore CreateCurrentStore()
            {
                return new CommerceCurrentStore(
                    StoreId,
                    "test-store",
                    "Test Store",
                    CommerceStoreStatuses.Active,
                    "https://test-store.example",
                    "test-store.example",
                    true,
                    null,
                    null,
                    "Test Store",
                    "support@test-store.example",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "USD",
                    "en-US",
                    "support@test-store.example",
                    null,
                    false,
                    null,
                    null);
            }
        }
    }
}
