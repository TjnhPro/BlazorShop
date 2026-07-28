namespace BlazorShop.Storefront.Presentation.Contracts
{
    public interface IStorefrontClientAppUrlResolver
    {
        string? ResolveBaseUrl();

        string ResolveUrl(string? relativeOrAbsoluteUrl);
    }
}