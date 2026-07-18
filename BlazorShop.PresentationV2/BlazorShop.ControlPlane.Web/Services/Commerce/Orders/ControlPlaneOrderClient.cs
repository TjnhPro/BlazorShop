namespace BlazorShop.ControlPlane.Web.Services.Commerce
{
    using System.Globalization;
    using System.Net.Http.Headers;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Domain.Contracts;

        public sealed class ControlPlaneOrderClient : ControlPlaneCommerceClientBase, IControlPlaneOrderClient
    {
        public ControlPlaneOrderClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<PagedResult<GetOrder>>(
                CommerceRoute(storePublicId, "orders") + BuildOrderQuery(query),
                "Unable to load orders.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}"),
                "Unable to load order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateOrderAdminNoteRequest, GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/admin-note"),
                request,
                "Unable to update order note.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateShippingStatusRequest, GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipping-status"),
                request,
                "Unable to update shipping status.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/complete"),
                "Unable to complete order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/cancel"),
                "Unable to cancel order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<GetShipment>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipment"),
                "Unable to load shipment.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpsertShipmentRequest, GetShipment>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipment"),
                request,
                "Unable to save shipment.",
                cancellationToken);
        }
    }
}

