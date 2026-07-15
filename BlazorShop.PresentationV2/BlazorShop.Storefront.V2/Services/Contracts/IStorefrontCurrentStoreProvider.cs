namespace BlazorShop.Storefront.Services.Contracts
{
    public interface IStorefrontCurrentStoreProvider
    {
        Task<StorefrontCurrentStoreResolution> ResolveAsync(CancellationToken cancellationToken = default);
    }

    public sealed record StorefrontCurrentStoreResolution(
        StorefrontCurrentStore? Store,
        StorefrontCurrentStoreResolutionStatus Status,
        string Message)
    {
        public static StorefrontCurrentStoreResolution Succeeded(StorefrontCurrentStore store)
        {
            ArgumentNullException.ThrowIfNull(store);

            return new(store, StorefrontCurrentStoreResolutionStatus.Success, "Current store resolved.");
        }

        public static StorefrontCurrentStoreResolution NotFound()
        {
            return new(null, StorefrontCurrentStoreResolutionStatus.NotFound, "The configured store was not found.");
        }

        public static StorefrontCurrentStoreResolution ServiceUnavailable()
        {
            return new(null, StorefrontCurrentStoreResolutionStatus.ServiceUnavailable, "The configured store could not be resolved.");
        }

        public static StorefrontCurrentStoreResolution Maintenance(StorefrontCurrentStore store)
        {
            ArgumentNullException.ThrowIfNull(store);

            return new(
                store,
                StorefrontCurrentStoreResolutionStatus.Maintenance,
                string.IsNullOrWhiteSpace(store.MaintenanceMessage)
                    ? "The store is temporarily unavailable for maintenance."
                    : store.MaintenanceMessage);
        }
    }

    public enum StorefrontCurrentStoreResolutionStatus
    {
        Success,
        NotFound,
        ServiceUnavailable,
        Maintenance,
    }
}
