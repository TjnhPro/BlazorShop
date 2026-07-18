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
    public sealed class ControlPlaneCommercePaymentsController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Payments.IControlPlanePaymentGateway gateway;

        public ControlPlaneCommercePaymentsController(BlazorShop.Application.ControlPlane.CommerceGateway.Payments.IControlPlanePaymentGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/payment-methods")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
        public async Task<IActionResult> ListPaymentMethods(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListPaymentMethodsAsync(storePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/payment-methods/{paymentMethodKey}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdatePaymentMethod(
            Guid storePublicId,
            string paymentMethodKey,
            [FromBody] UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdatePaymentMethodAsync(storePublicId, paymentMethodKey, request, cancellationToken));
        }
    }
}
