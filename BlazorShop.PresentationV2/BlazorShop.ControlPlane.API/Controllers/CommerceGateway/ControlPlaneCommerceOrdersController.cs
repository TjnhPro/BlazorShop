namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Globalization;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    [ApiController]
    [Route("api/control-plane/stores/{storePublicId:guid}/catalog")]
    [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
    public sealed class ControlPlaneCommerceOrdersController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Orders.IControlPlaneOrderGateway gateway;

        public ControlPlaneCommerceOrdersController(BlazorShop.Application.ControlPlane.CommerceGateway.Orders.IControlPlaneOrderGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders")]
        public async Task<IActionResult> QueryOrders(
            Guid storePublicId,
            [FromQuery] AdminOrderQueryDto query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.QueryOrdersAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}")]
        public async Task<IActionResult> GetOrder(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetOrderAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/admin-note")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateOrderAdminNote(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateOrderAdminNoteAsync(storePublicId, orderId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/shipping-status")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateOrderShippingStatus(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateOrderShippingStatusAsync(storePublicId, orderId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/complete")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CompleteOrder(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CompleteOrderAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/cancel")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CancelOrder(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CancelOrderAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/shipment")]
        public async Task<IActionResult> GetShipment(Guid storePublicId, Guid orderId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetShipmentAsync(storePublicId, orderId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/orders/{orderId:guid}/shipment")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpsertShipment(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpsertShipmentAsync(storePublicId, orderId, request, cancellationToken));
        }
    }
}
