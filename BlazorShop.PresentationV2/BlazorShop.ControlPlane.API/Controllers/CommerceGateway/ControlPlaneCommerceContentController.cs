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
    public sealed class ControlPlaneCommerceContentController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Content.IControlPlaneContentGateway gateway;

        public ControlPlaneCommerceContentController(BlazorShop.Application.ControlPlane.CommerceGateway.Content.IControlPlaneContentGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesRead)]
        public async Task<IActionResult> ListStorefrontPages(
            Guid storePublicId,
            [FromQuery] StorefrontPageListQuery query,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListStorefrontPagesAsync(storePublicId, query, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/templates")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesRead)]
        public async Task<IActionResult> ListStorefrontPageTemplates(
            Guid storePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListStorefrontPageTemplatesAsync(storePublicId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/template-status")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesRead)]
        public async Task<IActionResult> GetStorefrontPageTemplateStatus(
            Guid storePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetStorefrontPageTemplateStatusAsync(storePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> CreateStorefrontPage(
            Guid storePublicId,
            [FromBody] CreateStorefrontPageRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateStorefrontPageAsync(storePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/templates/{pageKey}/draft")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> CreateStorefrontPageDraftFromTemplate(
            Guid storePublicId,
            string pageKey,
            [FromBody] CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateStorefrontPageDraftFromTemplateAsync(storePublicId, pageKey, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesRead)]
        public async Task<IActionResult> GetStorefrontPage(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetStorefrontPageAsync(storePublicId, pagePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> UpdateStorefrontPage(
            Guid storePublicId,
            Guid pagePublicId,
            [FromBody] UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateStorefrontPageAsync(storePublicId, pagePublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}/template")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> MapStorefrontPageTemplate(
            Guid storePublicId,
            Guid pagePublicId,
            [FromBody] MapPageTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.MapStorefrontPageTemplateAsync(storePublicId, pagePublicId, request, cancellationToken));
        }

        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}/template")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> ClearStorefrontPageTemplate(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ClearStorefrontPageTemplateAsync(storePublicId, pagePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}/navigation")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> UpdateStorefrontPageNavigation(
            Guid storePublicId,
            Guid pagePublicId,
            [FromBody] UpdatePageNavigationRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateStorefrontPageNavigationAsync(storePublicId, pagePublicId, request, cancellationToken));
        }

        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommercePagesWrite)]
        public async Task<IActionResult> ArchiveStorefrontPage(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ArchiveStorefrontPageAsync(storePublicId, pagePublicId, cancellationToken));
        }
    }
}
