namespace BlazorShop.Storefront.Browser;

public interface IStorefrontAntiforgeryTokenReader
{
    ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default);
}
