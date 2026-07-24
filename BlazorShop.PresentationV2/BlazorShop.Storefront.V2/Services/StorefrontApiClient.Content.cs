namespace BlazorShop.Storefront.Services
{

    using BlazorShop.Storefront.Models;
using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public partial class StorefrontApiClient
    {
        public Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<GetStorefrontPage>(
                $"{StorefrontPagesBaseRoute}/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }
        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<StorefrontPageNavigationLinkDto>>(
                $"{StorefrontPagesBaseRoute}/navigation",
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>.Success([]);
        }
        public Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return Task.FromResult(StorefrontApiResult<StoreNavigationPublicMenuDto>.NotFound());
            }

            return GetMaybeNotFoundAsync<StoreNavigationPublicMenuDto>(
                $"{StorefrontNavigationBaseRoute}/{Uri.EscapeDataString(systemName.Trim().ToLowerInvariant())}",
                cancellationToken,
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback<GetSeoSettings>(
                SeoSettingsRoute,
                LegacySeoSettingsRoute,
                cancellationToken,
                requestTimeout: SeoSettingsRequestTimeout);
        }
        public Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(string path, CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<SeoRedirectResolutionDto>(
                $"{StorefrontSeoBaseRoute}/redirects/resolve?path={Uri.EscapeDataString(path)}",
                $"{LegacySeoRedirectsBaseRoute}/resolve?path={Uri.EscapeDataString(path)}",
                cancellationToken,
                RedirectResolutionRequestTimeout);
        }
    }
}
