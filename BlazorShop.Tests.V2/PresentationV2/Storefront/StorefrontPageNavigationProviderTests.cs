extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Presentation.Models;
    using StorefrontV2::BlazorShop.Storefront.Presentation.Services;
    using StorefrontV2::BlazorShop.Storefront.Presentation.Contracts;

    public sealed class StorefrontPageNavigationProviderTests
    {
        [Fact]
        public async Task GetLinksByLocationAsync_FiltersOrdersAndCachesWithinScope()
        {
            var apiClient = new StubContentClient(
                StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.Success(
                [
                    new StorefrontPageNavigationLinkDto("cookie_information", "cookies", "Cookie information", "footer_legal", 320),
                    new StorefrontPageNavigationLinkDto("terms_conditions", "terms", "Terms and conditions", "footer_legal", 300),
                    new StorefrontPageNavigationLinkDto("about", "about-us", "About us", "footer_company", 100),
                ]));
            var provider = new StorefrontPageNavigationProvider(apiClient);

            var first = await provider.GetLinksByLocationAsync("footer_legal");
            var second = await provider.GetLinksByLocationAsync("footer_legal");

            Assert.Equal(["terms", "cookies"], first.Select(link => link.Slug).ToArray());
            Assert.Equal(["terms", "cookies"], second.Select(link => link.Slug).ToArray());
            Assert.Equal(1, apiClient.RequestCount);
        }

        [Fact]
        public async Task GetLinksAsync_WhenNavigationEndpointUnavailable_ReturnsEmptyList()
        {
            var apiClient = new StubContentClient(
                StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.ServiceUnavailable());
            var provider = new StorefrontPageNavigationProvider(apiClient);

            var links = await provider.GetLinksAsync();

            Assert.Empty(links);
            Assert.Equal(1, apiClient.RequestCount);
        }

        private sealed class StubContentClient : IStorefrontContentClient
        {
            private readonly StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>> navigationResult;

            public StubContentClient(StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>> navigationResult)
            {
                this.navigationResult = navigationResult;
            }

            public int RequestCount { get; private set; }

            public Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(CancellationToken cancellationToken = default)
            {
                this.RequestCount++;
                return Task.FromResult(this.navigationResult);
            }

            public Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
                string systemName,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(string path, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
