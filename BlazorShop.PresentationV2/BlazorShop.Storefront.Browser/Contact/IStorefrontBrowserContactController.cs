using BlazorShop.Storefront.Components.Contracts.Contact;

namespace BlazorShop.Storefront.Browser.Contact;

public interface IStorefrontBrowserContactController
{
    Task<StorefrontContactFormSubmitResult> SubmitAsync(
        StorefrontContactFormSubmitRequest request,
        StorefrontContactFormActionDescriptor? actionDescriptor = null,
        CancellationToken cancellationToken = default);
}
