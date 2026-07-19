namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/categories/{categoryId:guid}/media")]
    public sealed class CommerceCategoryMediaController : CommerceAdminControllerBase
    {
        private readonly ICategoryMediaService categoryMediaService;

        public CommerceCategoryMediaController(ICategoryMediaService categoryMediaService)
        {
            this.categoryMediaService = categoryMediaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPrimary(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.categoryMediaService.GetPrimaryAsync(categoryId, cancellationToken);
            return this.FromCategoryMediaResult(result);
        }

        [HttpPut("primary")]
        public async Task<IActionResult> SetPrimary(
            Guid categoryId,
            [FromBody] SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.categoryMediaService.SetPrimaryAsync(categoryId, request, cancellationToken);
            return this.FromCategoryMediaResult(result);
        }

        [HttpDelete("primary")]
        public async Task<IActionResult> ClearPrimary(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.categoryMediaService.ClearPrimaryAsync(categoryId, cancellationToken);
            return this.FromCategoryMediaResult(result);
        }

        private IActionResult FromCategoryMediaResult<TPayload>(ApplicationResult<TPayload> result)
        {
            return result.ToCommerceNodeActionResult();
        }
    }
}
