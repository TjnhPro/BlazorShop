namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/products/{id:guid}/seo")]
    public sealed class ProductSeoController : CommerceAdminControllerBase
    {
        private readonly IProductSeoService productSeoService;

        public ProductSeoController(IProductSeoService productSeoService)
        {
            this.productSeoService = productSeoService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await this.productSeoService.GetByProductIdAsync(id);
            return this.FromServiceResponse(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(Guid id, UpdateProductSeoDto request)
        {
            var result = await this.productSeoService.UpdateAsync(id, request);
            return this.FromServiceResponse(result);
        }
    }
}
