extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging.Abstractions;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontCurrentStoreProviderTests
    {
        [Fact]
        public async Task ResolveAsync_WhenCurrentStoreSucceeds_CachesResultPerRequest()
        {
            var apiClient = new StubStoreConfigurationClient(StorefrontApiResult<StorefrontCurrentStore>.Success(CreateStore()));
            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext(),
            };
            var provider = new StorefrontCurrentStoreProvider(
                apiClient,
                accessor,
                NullLogger<StorefrontCurrentStoreProvider>.Instance);

            var first = await provider.ResolveAsync();
            var second = await provider.ResolveAsync();

            Assert.Equal(StorefrontCurrentStoreResolutionStatus.Success, first.Status);
            Assert.Same(first, second);
            Assert.Equal("default", first.Store?.StoreKey);
            Assert.Equal(1, apiClient.CurrentStoreCalls);
        }

        [Fact]
        public async Task ResolveAsync_WhenCurrentStoreIsMissing_ReturnsNotFound()
        {
            var apiClient = new StubStoreConfigurationClient(StorefrontApiResult<StorefrontCurrentStore>.NotFound());
            var provider = new StorefrontCurrentStoreProvider(
                apiClient,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                NullLogger<StorefrontCurrentStoreProvider>.Instance);

            var result = await provider.ResolveAsync();

            Assert.Equal(StorefrontCurrentStoreResolutionStatus.NotFound, result.Status);
            Assert.Null(result.Store);
            Assert.Equal(1, apiClient.CurrentStoreCalls);
        }

        [Fact]
        public async Task ResolveAsync_WhenCurrentStoreIsInMaintenance_ReturnsMaintenance()
        {
            var apiClient = new StubStoreConfigurationClient(
                StorefrontApiResult<StorefrontCurrentStore>.Success(CreateStore(maintenanceModeEnabled: true)));
            var provider = new StorefrontCurrentStoreProvider(
                apiClient,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                NullLogger<StorefrontCurrentStoreProvider>.Instance);

            var result = await provider.ResolveAsync();

            Assert.Equal(StorefrontCurrentStoreResolutionStatus.Maintenance, result.Status);
            Assert.Equal("Maintenance window.", result.Message);
        }

        private static StorefrontCurrentStore CreateStore(bool maintenanceModeEnabled = false)
        {
            return new StorefrontCurrentStore(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "default",
                "Default Store",
                "active",
                BaseUrl: "https://store.example/",
                PrimaryDomain: "store.example",
                ForceHttps: true,
                CdnHost: null,
                LogoUrl: null,
                CompanyName: null,
                CompanyEmail: null,
                CompanyPhone: null,
                CompanyAddress: null,
                FaviconUrl: null,
                PngIconUrl: null,
                AppleTouchIconUrl: null,
                MsTileImageUrl: null,
                MsTileColor: null,
                DefaultCurrencyCode: "USD",
                DefaultCulture: "en-US",
                SupportEmail: null,
                SupportPhone: null,
                MaintenanceModeEnabled: maintenanceModeEnabled,
                MaintenanceMessage: "Maintenance window.",
                HtmlBodyId: null);
        }

        private sealed class StubStoreConfigurationClient : IStorefrontStoreConfigurationClient
        {
            private readonly StorefrontApiResult<StorefrontCurrentStore> currentStoreResult;

            public StubStoreConfigurationClient(StorefrontApiResult<StorefrontCurrentStore> currentStoreResult)
            {
                this.currentStoreResult = currentStoreResult;
            }

            public int CurrentStoreCalls { get; private set; }

            public Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                this.CurrentStoreCalls++;
                return Task.FromResult(this.currentStoreResult);
            }

            public Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(StorefrontApiResult<StorefrontPublicConfiguration>.ServiceUnavailable());
            }

            public Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
                StorefrontCurrencyPreferenceRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed("Configuration unavailable."));
            }
        }
    }
}
