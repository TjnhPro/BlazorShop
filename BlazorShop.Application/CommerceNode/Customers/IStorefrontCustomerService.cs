namespace BlazorShop.Application.CommerceNode.Customers
{
    using BlazorShop.Application.DTOs;

    public interface IStorefrontCustomerService
    {
        Task<ServiceResponse<StorefrontCustomerProfile>> ResolveOrCreateAsync(
            StorefrontCustomerResolutionRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCustomerProfile>> TouchLastActivityAsync(
            Guid storeId,
            string appUserId,
            DateTimeOffset? activityAtUtc = null,
            CancellationToken cancellationToken = default);
    }
}
