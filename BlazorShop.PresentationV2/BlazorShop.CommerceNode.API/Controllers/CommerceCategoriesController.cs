namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/categories")]
    public sealed class CommerceCategoriesController : CommerceAdminControllerBase
    {
        private readonly ICategoryService categoryService;

        public CommerceCategoriesController(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await this.categoryService.GetAllAsync();
            return this.Success(categories, "Categories retrieved successfully.");
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await this.categoryService.GetByIdAsync(id);
            return category.Id == Guid.Empty
                ? this.Failure<object>(ServiceResponseType.NotFound, "Category not found.")
                : this.Success(category, "Category retrieved successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCategory category)
        {
            var result = await this.categoryService.AddAsync(category);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateCategory category)
        {
            category.Id = id;
            var result = await this.categoryService.UpdateAsync(category);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await this.categoryService.DeleteAsync(id);
            return this.FromServiceResponse(result);
        }
    }
}
