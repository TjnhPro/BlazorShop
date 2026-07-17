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

        [Fact]
        public async Task FreeStandardProvider_AddsProductSurchargeAndExcludesFreeShippingLines()
        {
            var provider = new InternalFreeStandardShippingProvider();

            var result = await provider.GetOptionsAsync(CreateRequest(
                shippingRequired: true,
                packageLines:
                [
                    CreatePackageLine(quantity: 2, shippingRequired: true, freeShipping: false, surcharge: 2.5m),
                    CreatePackageLine(quantity: 1, shippingRequired: true, freeShipping: true, surcharge: 100m),
                ]));

            var option = Assert.Single(result.Options);
            Assert.Equal(5m, option.Rate);
            Assert.Equal("product.surcharge.sum", option.RuleMatch);
        }

        [Fact]
        public async Task FlatRateProvider_UsesHighestSurchargePolicy()
        {
            var provider = new InternalFlatRateShippingProvider(new StubStoreShippingSettingsService(
                defaultFlatRate: 5m,
                freeShippingThreshold: null,
                StoreShippingSurchargePolicies.Highest));

            var result = await provider.GetOptionsAsync(CreateRequest(
                shippingRequired: true,
                packageLines:
                [
                    CreatePackageLine(quantity: 3, shippingRequired: true, freeShipping: false, surcharge: 2m),
                    CreatePackageLine(quantity: 1, shippingRequired: true, freeShipping: false, surcharge: 7m),
                ]));

            var option = Assert.Single(result.Options);
            Assert.Equal(12m, option.Rate);
            Assert.Equal("store.flat_rate+product.surcharge.highest", option.RuleMatch);
        }

        [Fact]
        public async Task FlatRateProvider_FreeShippingThresholdWaivesBaseRateAndSurcharge()
        {
            var provider = new InternalFlatRateShippingProvider(new StubStoreShippingSettingsService(
                defaultFlatRate: 5m,
                freeShippingThreshold: 20m,
                StoreShippingSurchargePolicies.Sum));

            var result = await provider.GetOptionsAsync(CreateRequest(
                shippingRequired: true,
                subtotal: 20m,
                packageLines:
                [
                    CreatePackageLine(quantity: 1, shippingRequired: true, freeShipping: false, surcharge: 7m),
                ]));

            var option = Assert.Single(result.Options);
            Assert.Equal(0m, option.Rate);
            Assert.Equal("store.free_shipping_threshold", option.RuleMatch);
        }

        private static ShippingOptionsRequest CreateRequest(
            bool shippingRequired,
            decimal subtotal = 20m,
            IReadOnlyList<ShippingPackageLine>? packageLines = null)
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
                Subtotal: subtotal,
                PackageLines: packageLines ??
                [
                    CreatePackageLine(quantity: 1, shippingRequired, freeShipping: false, surcharge: null),
                ]);
        }

        private static ShippingPackageLine CreatePackageLine(
            int quantity,
            bool shippingRequired,
            bool freeShipping,
            decimal? surcharge)
        {
            return new ShippingPackageLine(
                ProductId: Guid.NewGuid(),
                ProductVariantId: null,
                Quantity: quantity,
                ShippingRequired: shippingRequired,
                FreeShipping: freeShipping,
                Weight: 1.25m,
                Length: 10m,
                Width: 5m,
                Height: 2m,
                Surcharge: surcharge);
        }

        private sealed class StubStoreShippingSettingsService : IStoreShippingSettingsService
        {
            private readonly StoreShippingRuntimeSettings settings;

            public StubStoreShippingSettingsService(
                decimal? defaultFlatRate,
                decimal? freeShippingThreshold,
                string surchargePolicy)
            {
                this.settings = new StoreShippingRuntimeSettings(
                    new StoreShippingOriginDto(null, null, null, null, null, null, null, "US"),
                    [],
                    defaultFlatRate,
                    freeShippingThreshold,
                    surchargePolicy,
                    "Standard delivery");
            }

            public Task<ServiceResponse<StoreShippingSettingsDto>> GetAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<StoreShippingSettingsDto>> UpdateAsync(
                UpdateStoreShippingSettingsRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StoreShippingRuntimeSettings> ResolveAsync(Guid storeId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.settings);
            }

            public Task<StoreShippingRuntimeSettings> ResolveCurrentAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.settings);
            }
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
