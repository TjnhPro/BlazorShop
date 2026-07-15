namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.DTOs;

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

        private IActionResult FromCategoryMediaResult<TPayload>(CategoryMediaOperationResult<TPayload> result)
        {
            if (result.Success)
            {
                return this.Success(result.Payload, result.Message ?? "Category media request completed.");
            }

            return this.Failure<TPayload>(
                ToServiceResponseType(result.Failure),
                result.Message ?? "Category media request could not be completed.",
                result.Payload);
        }

        private static ServiceResponseType ToServiceResponseType(CategoryMediaOperationFailure? failure)
        {
            return failure switch
            {
                CategoryMediaOperationFailure.Validation => ServiceResponseType.ValidationError,
                CategoryMediaOperationFailure.NotFound => ServiceResponseType.NotFound,
                CategoryMediaOperationFailure.Conflict => ServiceResponseType.Conflict,
                _ => ServiceResponseType.Failure,
            };
        }
    }
}
