namespace BlazorShop.Application.CommerceNode.Currencies
{
    public interface IStoreCurrencyResolver
    {
        Task<string> ResolveDefaultCurrencyCodeAsync(
            Guid storeId,
            CancellationToken cancellationToken = default);
    }
}
