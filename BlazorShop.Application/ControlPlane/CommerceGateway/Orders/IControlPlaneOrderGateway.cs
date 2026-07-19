namespace BlazorShop.Application.ControlPlane.CommerceGateway.Orders
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public interface IControlPlaneOrderGateway
    {
        
                Task<ApplicationResult<PagedResult<GetOrder>>> QueryOrdersAsync(
                    Guid storePublicId,
                    AdminOrderQueryDto query,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetOrder>> GetOrderAsync(
                    Guid storePublicId,
                    Guid orderId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetOrder>> UpdateOrderAdminNoteAsync(
                    Guid storePublicId,
                    Guid orderId,
                    UpdateOrderAdminNoteRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetOrder>> UpdateOrderShippingStatusAsync(
                    Guid storePublicId,
                    Guid orderId,
                    UpdateShippingStatusRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetOrder>> CompleteOrderAsync(
                    Guid storePublicId,
                    Guid orderId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetOrder>> CancelOrderAsync(
                    Guid storePublicId,
                    Guid orderId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetShipment>> GetShipmentAsync(
                    Guid storePublicId,
                    Guid orderId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<GetShipment>> UpsertShipmentAsync(
                    Guid storePublicId,
                    Guid orderId,
                    UpsertShipmentRequest request,
                    CancellationToken cancellationToken = default);
    }
}

