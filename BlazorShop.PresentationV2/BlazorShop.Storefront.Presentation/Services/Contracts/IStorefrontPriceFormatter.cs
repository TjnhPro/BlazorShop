namespace BlazorShop.Storefront.Presentation.Contracts
{
    public interface IStorefrontPriceFormatter
    {
        string Format(decimal amount, StorefrontDisplayContext displayContext);
    }
}
