namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/products/{productId:guid}/variants")]
    public sealed class CommerceProductVariantsController : CommerceAdminControllerBase
    {
        private readonly IProductVariantService variantService;

        public CommerceProductVariantsController(IProductVariantService variantService)
        {
            this.variantService = variantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByProductId(Guid productId)
        {
            var variants = await this.variantService.GetByProductIdAsync(productId);
            return this.Success(variants, "Product variants retrieved successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, CreateProductVariant variant)
        {
            variant.ProductId = productId;
            var result = await this.variantService.AddAsync(variant);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{variantId:guid}")]
        public async Task<IActionResult> Update(Guid productId, Guid variantId, UpdateProductVariant variant)
        {
            variant.ProductId = productId;
            variant.Id = variantId;
            var result = await this.variantService.UpdateAsync(variant);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{variantId:guid}")]
        public async Task<IActionResult> Delete(Guid variantId)
        {
            var result = await this.variantService.DeleteAsync(variantId);
            return this.FromServiceResponse(result);
        }
    }
}
