namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.Services.Contracts.Admin;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/inventory")]
    public sealed class InventoryController : CommerceAdminControllerBase
    {
        private readonly IAdminInventoryService inventoryService;

        public InventoryController(IAdminInventoryService inventoryService)
        {
            this.inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AdminInventoryQueryDto query)
        {
            var inventory = await this.inventoryService.GetAsync(query);
            return this.Success(inventory, "Inventory retrieved successfully.");
        }

        [HttpPut("products/{productId:guid}")]
        public async Task<IActionResult> UpdateProductStock(Guid productId, UpdateProductStockDto request)
        {
            var result = await this.inventoryService.UpdateProductStockAsync(productId, request);
            return this.FromServiceResponse(result);
        }

        [HttpPut("variants/{variantId:guid}")]
        public async Task<IActionResult> UpdateVariantStock(Guid variantId, UpdateVariantStockDto request)
        {
            var result = await this.inventoryService.UpdateVariantStockAsync(variantId, request);
            return this.FromServiceResponse(result);
        }
    }
}
