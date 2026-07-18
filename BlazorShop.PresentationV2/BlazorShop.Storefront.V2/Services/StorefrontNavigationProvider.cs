namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontNavigationProvider : IStorefrontNavigationProvider
    {
        private readonly IStorefrontContentClient apiClient;
        private readonly Dictionary<string, Task<StoreNavigationPublicMenuDto?>> menuTasks = new(StringComparer.OrdinalIgnoreCase);

        public StorefrontNavigationProvider(IStorefrontContentClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<StoreNavigationPublicMenuDto?> GetMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return null;
            }

            var normalizedSystemName = systemName.Trim().ToLowerInvariant();
            if (!this.menuTasks.TryGetValue(normalizedSystemName, out var task))
            {
                task = this.LoadMenuAsync(normalizedSystemName, cancellationToken);
                this.menuTasks[normalizedSystemName] = task;
            }

            return await task;
        }

        private async Task<StoreNavigationPublicMenuDto?> LoadMenuAsync(
            string systemName,
            CancellationToken cancellationToken)
        {
            var result = await this.apiClient.GetNavigationMenuAsync(systemName, cancellationToken);
            return result.IsSuccess && result.Value is not null && result.Value.Items.Count > 0
                ? result.Value
                : null;
        }
    }
}
