namespace BlazorShop.Application.CommerceNode.Orders
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;

    public sealed record StorefrontGuestOrderLookupRequest(
        string Reference,
        string AccessToken);

    public interface IStorefrontGuestOrderService
    {
        Task<ServiceResponse<GetOrder>> GetAsync(
            StorefrontGuestOrderLookupRequest request,
            CancellationToken cancellationToken = default);
    }
}
