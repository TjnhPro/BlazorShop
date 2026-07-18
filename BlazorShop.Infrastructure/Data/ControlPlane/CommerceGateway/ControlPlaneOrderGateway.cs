namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public sealed class ControlPlaneOrderGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Orders.IControlPlaneOrderGateway
    {
        public ControlPlaneOrderGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetOrder>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/orders" + BuildOrderQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/orders/{orderId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/orders/{orderId:D}/admin-note",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/orders/{orderId:D}/shipping-status",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/orders/{orderId:D}/complete",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/orders/{orderId:D}/cancel",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetShipment>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/orders/{orderId:D}/shipment",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetShipment>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/orders/{orderId:D}/shipment",
                request,
                cancellationToken);
        }
    }
}

