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
    public sealed class ControlPlaneCommerceCategoriesController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Categories.IControlPlaneCategoryGateway gateway;

        public ControlPlaneCommerceCategoriesController(BlazorShop.Application.ControlPlane.CommerceGateway.Categories.IControlPlaneCategoryGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}/seo")]
        public async Task<IActionResult> GetCategorySeo(Guid storePublicId, Guid categoryId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetCategorySeoAsync(storePublicId, categoryId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}/seo")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateCategorySeo(
            Guid storePublicId,
            Guid categoryId,
            [FromBody] UpdateCategorySeoDto request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateCategorySeoAsync(storePublicId, categoryId, request, cancellationToken));
        }

        [HttpGet("categories")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories")]
        public async Task<IActionResult> ListCategories(
            Guid storePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.ListCategoriesAsync(storePublicId, pageNumber, pageSize, cancellationToken));
        }

        [HttpGet("categories/tree")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/tree")]
        public async Task<IActionResult> GetCategoryTree(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetCategoryTreeAsync(storePublicId, cancellationToken));
        }

        [HttpPost("categories")]
        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CreateCategory(
            Guid storePublicId,
            [FromBody] CreateCategory request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CreateCategoryAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("categories/{categoryId:guid}")]
        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> UpdateCategory(
            Guid storePublicId,
            Guid categoryId,
            [FromBody] UpdateCategory request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateCategoryAsync(storePublicId, categoryId, request, cancellationToken));
        }

        [HttpDelete("categories/{categoryId:guid}")]
        [HttpDelete("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ArchiveCategory(Guid storePublicId, Guid categoryId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ArchiveCategoryAsync(storePublicId, categoryId, cancellationToken));
        }

        [HttpGet("categories/{categoryId:guid}/media")]
        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/categories/{categoryId:guid}/media")]
        public async Task<IActionResult> GetCategoryMedia(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetCategoryMediaAsync(storePublicId, categoryId, cancellationToken));
        }
    }
}
