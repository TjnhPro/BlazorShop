namespace BlazorShop.Application.CommerceNode.Stores
{
    public static class CommerceStoreContextExtensions
    {
        public static async Task<Guid?> GetCurrentStoreIdOrDefaultAsync(
            this ICommerceStoreContext storeContext,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(storeContext);

            var result = await storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success && result.Payload != Guid.Empty ? result.Payload : null;
        }
    }
}
