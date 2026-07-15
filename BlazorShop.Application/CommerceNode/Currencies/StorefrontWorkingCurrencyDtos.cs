namespace BlazorShop.Application.CommerceNode.Currencies
{
    public sealed record StorefrontWorkingCurrencyResolution(
        string CurrencyCode,
        string BaseCurrencyCode,
        string? RequestedCurrencyCode,
        bool RequestedCurrencySupported,
        bool CheckoutCurrencyEnabled,
        string Reason);

    public interface IStorefrontWorkingCurrencyResolver
    {
        Task<StorefrontWorkingCurrencyResolution> ResolveAsync(
            Guid storeId,
            string? requestedCurrencyCode,
            CancellationToken cancellationToken = default);
    }
}
