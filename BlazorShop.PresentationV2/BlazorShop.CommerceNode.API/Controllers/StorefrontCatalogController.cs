namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/internal/catalog")]
    public sealed class StorefrontCatalogController : StorefrontInternalControllerBase
    {
        private readonly IPublicCatalogService publicCatalogService;

        public StorefrontCatalogController(IPublicCatalogService publicCatalogService)
        {
            this.publicCatalogService = publicCatalogService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoriesAsync();
            return this.Success(categories, "Published categories loaded.");
        }

        [HttpGet("categories/tree")]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoryTreeAsync();
            return this.Success(categories, "Published category tree loaded.");
        }

        [HttpGet("categories/{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(id);
            return category is null
                ? this.Failure<GetCategory>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(category, "Published category loaded.");
        }

        [HttpGet("categories/slug/{slug}")]
        public async Task<IActionResult> GetCategoryBySlug(string slug)
        {
            var categoryPage = await this.publicCatalogService.GetPublishedCategoryPageBySlugAsync(slug);
            return categoryPage is null
                ? this.Failure<GetCategoryPage>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(categoryPage, "Published category page loaded.");
        }

        [HttpGet("categories/{categoryId:guid}/products")]
        public async Task<IActionResult> GetProductsByCategory(Guid categoryId)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(categoryId);
            if (category is null)
            {
                return this.Failure<IReadOnlyList<GetCatalogProduct>>(
                    ServiceResponseType.NotFound,
                    "Published category was not found.");
            }

            var products = await this.publicCatalogService.GetPublishedProductsByCategoryAsync(categoryId);
            return this.Success(products, "Published category products loaded.");
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] ProductCatalogQuery query)
        {
            var products = await this.publicCatalogService.GetPublishedCatalogPageAsync(query);
            return this.Success(products, "Published products loaded.");
        }

        [HttpGet("products/{id:guid}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await this.publicCatalogService.GetPublishedProductByIdAsync(id);
            return product is null
                ? this.Failure<GetProduct>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(product, "Published product loaded.");
        }

        [HttpGet("products/slug/{slug}")]
        public async Task<IActionResult> GetProductBySlug(string slug)
        {
            var product = await this.publicCatalogService.GetPublishedProductBySlugAsync(slug);
            return product is null
                ? this.Failure<GetProduct>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(product, "Published product loaded.");
        }

        [HttpGet("sitemap")]
        public async Task<IActionResult> GetSitemap()
        {
            var sitemap = await this.publicCatalogService.GetPublishedSitemapAsync();
            return this.Success<GetPublicCatalogSitemap>(sitemap, "Published catalog sitemap loaded.");
        }
    }
}
