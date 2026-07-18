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
    public sealed class ControlPlaneCommerceNavigationController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Navigation.IControlPlaneNavigationGateway gateway;

        public ControlPlaneCommerceNavigationController(BlazorShop.Application.ControlPlane.CommerceGateway.Navigation.IControlPlaneNavigationGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/menus")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationRead)]
        public async Task<IActionResult> ListNavigationMenus(
            Guid storePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListNavigationMenusAsync(storePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/menus")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationWrite)]
        public async Task<IActionResult> CreateNavigationMenu(
            Guid storePublicId,
            [FromBody] CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateNavigationMenuAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/menus/{menuPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationRead)]
        public async Task<IActionResult> GetNavigationMenu(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetNavigationMenuAsync(storePublicId, menuPublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/menus/{menuPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationWrite)]
        public async Task<IActionResult> UpdateNavigationMenu(
            Guid storePublicId,
            Guid menuPublicId,
            [FromBody] UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateNavigationMenuAsync(storePublicId, menuPublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/menus/{menuPublicId:guid}/items")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationWrite)]
        public async Task<IActionResult> CreateNavigationItem(
            Guid storePublicId,
            Guid menuPublicId,
            [FromBody] CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateNavigationItemAsync(storePublicId, menuPublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/items/{itemPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationWrite)]
        public async Task<IActionResult> UpdateNavigationItem(
            Guid storePublicId,
            Guid itemPublicId,
            [FromBody] UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateNavigationItemAsync(storePublicId, itemPublicId, request, cancellationToken));
        }

        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/items/{itemPublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationWrite)]
        public async Task<IActionResult> ArchiveNavigationItem(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ArchiveNavigationItemAsync(storePublicId, itemPublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/menus/{menuPublicId:guid}/items/order")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationWrite)]
        public async Task<IActionResult> UpdateNavigationItemOrder(
            Guid storePublicId,
            Guid menuPublicId,
            [FromBody] UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateNavigationItemOrderAsync(storePublicId, menuPublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/system-targets")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceNavigationRead)]
        public async Task<IActionResult> ListNavigationSystemTargets(
            Guid storePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListNavigationSystemTargetsAsync(storePublicId, cancellationToken));
        }
    }
}
