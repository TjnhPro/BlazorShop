namespace BlazorShop.Storefront.Components.Browser;

public interface IStorefrontAntiforgeryTokenReader
{
    ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default);
}
