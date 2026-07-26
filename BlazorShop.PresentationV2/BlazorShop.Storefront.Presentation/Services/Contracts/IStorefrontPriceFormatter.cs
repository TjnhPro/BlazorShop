namespace BlazorShop.Storefront.Services.Contracts
{
    public interface IStorefrontPriceFormatter
    {
        string Format(decimal amount, StorefrontDisplayContext displayContext);
    }
}
