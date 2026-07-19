namespace BlazorShop.Application.CommerceNode.Stores
{
    using BlazorShop.Application.Common.Results;

    public interface ICommerceStoreContext
    {
        Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
            CancellationToken cancellationToken = default);

        Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(
            CancellationToken cancellationToken = default);
    }
}
