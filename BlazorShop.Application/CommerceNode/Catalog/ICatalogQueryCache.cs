namespace BlazorShop.Application.CommerceNode.Catalog
{
    public interface ICatalogQueryCache
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

        Task InvalidateStoreCatalogAsync(Guid storeId, CancellationToken cancellationToken = default);
    }
}
