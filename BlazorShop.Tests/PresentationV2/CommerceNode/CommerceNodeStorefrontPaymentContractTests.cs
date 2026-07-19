extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Text;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;

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
    }
}
