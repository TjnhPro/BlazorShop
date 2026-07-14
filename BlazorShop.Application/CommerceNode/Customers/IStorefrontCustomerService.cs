namespace BlazorShop.Application.CommerceNode.Customers
{
    using BlazorShop.Application.DTOs;

    public interface IStorefrontCustomerService
    {
        Task<ServiceResponse<StorefrontCustomerProfile>> ResolveOrCreateAsync(
            StorefrontCustomerResolutionRequest request,
            CancellationToken cancellationToken = default);
    }
}
