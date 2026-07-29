namespace BlazorShop.Storefront.Browser;

public interface IStorefrontBrowserCartEventPublisher
{
    ValueTask PublishCartChangedAsync(int count, CancellationToken cancellationToken = default);
}
