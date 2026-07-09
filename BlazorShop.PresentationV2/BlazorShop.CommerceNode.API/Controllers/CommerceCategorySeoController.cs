namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/categories/{id:guid}/seo")]
    public sealed class CommerceCategorySeoController : CommerceAdminControllerBase
    {
        private readonly ICategorySeoService categorySeoService;

        public CommerceCategorySeoController(ICategorySeoService categorySeoService)
        {
            this.categorySeoService = categorySeoService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await this.categorySeoService.GetByCategoryIdAsync(id);
            return this.FromServiceResponse(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(Guid id, UpdateCategorySeoDto request)
        {
            var result = await this.categorySeoService.UpdateAsync(id, request);
            return this.FromServiceResponse(result);
        }
    }
}
