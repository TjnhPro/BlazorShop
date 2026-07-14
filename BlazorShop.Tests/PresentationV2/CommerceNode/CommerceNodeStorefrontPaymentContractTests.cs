extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Domain.Entities;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
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
        public async Task CapturePayPal_WhenTokenMissing_ReturnsTypedValidationError()
        {
            using var client = this.CreateClient(captureResult: true);

            using var response = await client.PostAsJsonAsync(
                "/api/storefront/stores/test-store/payments/paypal/capture",
                new { token = " " });
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("validation.failed", body.RootElement.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
            Assert.True(body.RootElement.TryGetProperty("fieldErrors", out _));
        }

        [Fact]
        public async Task CapturePayPal_WhenProviderCaptureFails_ReturnsTypedConflictError()
        {
            using var client = this.CreateClient(captureResult: false);

            using var response = await client.PostAsJsonAsync(
                "/api/storefront/stores/test-store/payments/paypal/capture",
                new { token = "demo-token" });
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("payment.paypal_capture_failed", body.RootElement.GetProperty("code").GetString());
            Assert.False(body.RootElement.TryGetProperty("data", out _));
        }

        [Fact]
        public async Task CapturePayPal_WhenCalledWithGet_ReturnsMethodNotAllowed()
        {
            using var client = this.CreateClient(captureResult: true);

            using var response = await client.GetAsync(
                "/api/storefront/stores/test-store/payments/paypal/capture?token=demo-token");

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        private HttpClient CreateClient(bool captureResult)
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPayPalPaymentService>();
                    services.AddScoped<IPayPalPaymentService>(_ => new StubPayPalPaymentService(captureResult));
                });
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private sealed class StubPayPalPaymentService : IPayPalPaymentService
        {
            private readonly bool captureResult;

            public StubPayPalPaymentService(bool captureResult)
            {
                this.captureResult = captureResult;
            }

            public Task<ServiceResponse> Pay(
                decimal totalAmount,
                IEnumerable<Product> cartProducts,
                IEnumerable<ProcessCart> carts)
            {
                return Task.FromResult(new ServiceResponse(false, "PayPal test stub does not implement payment creation."));
            }

            public Task<bool> CaptureAsync(string orderId)
            {
                return Task.FromResult(this.captureResult);
            }
        }
    }
}
