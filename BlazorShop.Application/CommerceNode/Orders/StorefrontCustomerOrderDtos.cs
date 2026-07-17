namespace BlazorShop.Application.CommerceNode.Orders
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Domain.Contracts;

    public sealed record StorefrontCustomerOrderQuery(
        string AppUserId,
        int PageNumber = 1,
        int PageSize = 10);

    public sealed record StorefrontCustomerOrderLookupRequest(
        string AppUserId,
        string OrderReference);

    public interface IStorefrontCustomerOrderService
    {
        Task<ServiceResponse<PagedResult<GetOrder>>> ListAsync(
            StorefrontCustomerOrderQuery query,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<GetOrder>> GetAsync(
            StorefrontCustomerOrderLookupRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<GetOrder>> GetReceiptAsync(
            StorefrontCustomerOrderLookupRequest request,
            CancellationToken cancellationToken = default);
    }
}
