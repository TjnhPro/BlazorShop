namespace BlazorShop.Application.Services.Contracts.Admin
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;

    public interface IAdminShipmentService
    {
        Task<ServiceResponse<GetShipment>> GetShipmentAsync(Guid orderId);

        Task<ServiceResponse<GetShipment>> UpsertShipmentAsync(Guid orderId, UpsertShipmentRequest request);
    }
}
