namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/products")]
    public sealed class ProductsController : CommerceAdminControllerBase
    {
        private readonly IProductService productService;

        public ProductsController(IProductService productService)
        {
            this.productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await this.productService.GetAllAsync();
            return this.Success(products, "Products retrieved successfully.");
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await this.productService.GetByIdAsync(id);
            return product is null
                ? this.Failure<object>(ServiceResponseType.NotFound, "Product not found.")
                : this.Success(product, "Product retrieved successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProduct product)
        {
            var result = await this.productService.AddAsync(product);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateProduct product)
        {
            product.Id = id;
            var result = await this.productService.UpdateAsync(product);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await this.productService.DeleteAsync(id);
            return this.FromServiceResponse(result);
        }
    }
}
