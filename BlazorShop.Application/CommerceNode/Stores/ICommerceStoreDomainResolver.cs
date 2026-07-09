namespace BlazorShop.Application.CommerceNode.Stores
{
    public interface ICommerceStoreDomainResolver
    {
        Task<CommerceStoreOperationResult<CommerceCurrentStore>> ResolveAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default);

        Task<CommerceStoreOperationResult<Guid>> ResolveStoreIdAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default);
    }
}
