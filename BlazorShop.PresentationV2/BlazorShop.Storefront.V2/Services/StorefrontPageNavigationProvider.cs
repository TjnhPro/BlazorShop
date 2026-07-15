namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontPageNavigationProvider : IStorefrontPageNavigationProvider
    {
        private readonly StorefrontApiClient apiClient;
        private Task<IReadOnlyList<StorefrontPageNavigationLinkDto>>? linksTask;

        public StorefrontPageNavigationProvider(StorefrontApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksAsync(
            CancellationToken cancellationToken = default)
        {
            this.linksTask ??= this.LoadLinksAsync(cancellationToken);
            return await this.linksTask;
        }

        public async Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksByLocationAsync(
            string navigationLocation,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(navigationLocation))
            {
                return [];
            }

            var normalizedLocation = navigationLocation.Trim();
            var links = await this.GetLinksAsync(cancellationToken);
            return links
                .Where(link => string.Equals(link.NavigationLocation, normalizedLocation, StringComparison.Ordinal))
                .OrderBy(link => link.DisplayOrder)
                .ThenBy(link => link.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private async Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> LoadLinksAsync(
            CancellationToken cancellationToken)
        {
            var result = await this.apiClient.GetPageNavigationLinksAsync(cancellationToken);
            return result.IsSuccess && result.Value is not null
                ? result.Value
                : [];
        }
    }
}
