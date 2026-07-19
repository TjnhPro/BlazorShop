namespace BlazorShop.Application.CommerceNode.Stores
{
    using BlazorShop.Application.Common.Results;

    public interface ICommerceStoreDomainResolver
    {
        Task<ApplicationResult<CommerceCurrentStore>> ResolveAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<StoreExecutionContext>> ResolveExecutionContextAsync(
            string? storeKey = null,
            string? host = null,
            string source = StoreExecutionContextSources.Unknown,
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<Guid>> ResolveStoreIdAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default);
    }
}
