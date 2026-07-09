namespace BlazorShop.Application.CommerceNode.Stores
{
    public interface ICommerceStoreContext
    {
        Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
            CancellationToken cancellationToken = default);
    }
}
