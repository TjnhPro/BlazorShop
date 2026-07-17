namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class ShippingProviderRegistryTests
    {
        [Fact]
        public async Task FreeStandardProvider_ReturnsOption_WhenShippingIsRequired()
        {
            var provider = new InternalFreeStandardShippingProvider();

            var result = await provider.GetOptionsAsync(CreateRequest(shippingRequired: true));

            var option = Assert.Single(result.Options);
            Assert.Equal("free_standard", option.Key);
            Assert.Equal("free_standard", option.ProviderSystemName);
            Assert.Equal("standard", option.MethodCode);
            Assert.Equal(0m, option.Rate);
            Assert.Equal("USD", option.CurrencyCode);
            Assert.Equal("internal.free_standard", option.RuleMatch);
            Assert.Empty(option.Warnings);
            Assert.Empty(option.Errors);
        }

        [Fact]
        public async Task Calculator_ReturnsNoShippingRequired_WhenNoPackageLinesNeedShipping()
        {
            var calculator = new ShippingCalculator([new InternalFreeStandardShippingProvider()]);

            var result = await calculator.GetOptionsAsync(CreateRequest(shippingRequired: false));

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.NotNull(result.Payload);
            Assert.False(result.Payload!.ShippingRequired);
            Assert.Empty(result.Payload.Options);
            Assert.Empty(result.Payload.Warnings);
            Assert.Empty(result.Payload.Errors);
        }

        [Fact]
        public void Resolver_RejectsUnknownProvider()
        {
            var resolver = new ShippingProviderResolver([new InternalFreeStandardShippingProvider()]);

            var result = resolver.Resolve("unknown_provider");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Shipping provider is not supported.", result.Message);
        }

        [Fact]
        public async Task Calculator_PreservesProviderWarningsAndErrors()
        {
            var calculator = new ShippingCalculator([new WarningShippingProvider()]);

            var result = await calculator.GetOptionsAsync(CreateRequest(shippingRequired: true));

            Assert.True(result.Success);
            Assert.NotNull(result.Payload);
            Assert.True(result.Payload!.ShippingRequired);
            Assert.Empty(result.Payload.Options);
            Assert.Equal(["country not configured"], result.Payload.Warnings);
            Assert.Equal(["rate unavailable"], result.Payload.Errors);
        }

        private static ShippingOptionsRequest CreateRequest(bool shippingRequired)
        {
            return new ShippingOptionsRequest(
                StoreId: Guid.NewGuid(),
                CartId: Guid.NewGuid(),
                CartPublicId: Guid.NewGuid(),
                Address: new ShippingAddressSnapshot(
                    FullName: "Customer One",
                    Company: null,
                    Address1: "100 Main St",
                    Address2: null,
                    City: "New York",
                    StateProvinceCode: "NY",
                    PostalCode: "10001",
                    CountryCode: "US",
                    Phone: "5550100",
                    Email: "customer@example.test"),
                CurrencyCode: "usd",
                Subtotal: 20m,
                PackageLines:
                [
                    new ShippingPackageLine(
                        ProductId: Guid.NewGuid(),
                        ProductVariantId: null,
                        Quantity: 1,
                        ShippingRequired: shippingRequired,
                        FreeShipping: false,
                        Weight: 1.25m,
                        Length: 10m,
                        Width: 5m,
                        Height: 2m,
                        Surcharge: null),
                ]);
        }

        private sealed class WarningShippingProvider : IShippingProvider
        {
            public string ProviderSystemName => "warning_provider";

            public Task<ShippingProviderResult> GetOptionsAsync(
                ShippingOptionsRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ShippingProviderResult(
                    Options: [],
                    Warnings: ["country not configured"],
                    Errors: ["rate unavailable"]));
            }
        }
    }
}
