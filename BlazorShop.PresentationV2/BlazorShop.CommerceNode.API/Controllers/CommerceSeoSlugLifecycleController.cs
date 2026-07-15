namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/seo/slugs")]
    public sealed class CommerceSeoSlugLifecycleController : CommerceAdminControllerBase
    {
        private readonly ICommerceStoreContext storeContext;
        private readonly IStoreSeoSlugHistoryService historyService;
        private readonly IStoreSeoSlugPolicyService policyService;

        public CommerceSeoSlugLifecycleController(
            ICommerceStoreContext storeContext,
            IStoreSeoSlugHistoryService historyService,
            IStoreSeoSlugPolicyService policyService)
        {
            this.storeContext = storeContext;
            this.historyService = historyService;
            this.policyService = policyService;
        }

        [HttpPost("generate")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Generate(
            [FromBody] StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.StoreScopeFailure<StoreSeoSlugPolicyResult>(storeResult);
            }

            var result = await this.policyService.GenerateSlugAsync(
                request.EntityType ?? string.Empty,
                request.SourceName,
                storeResult.Payload,
                request.LanguageCode,
                request.ExcludedEntityId,
                cancellationToken);

            return this.Success(result, result.Success ? "SEO slug generated." : "SEO slug could not be generated.");
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Validate(
            [FromBody] StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.StoreScopeFailure<StoreSeoSlugPolicyResult>(storeResult);
            }

            var result = await this.policyService.ValidateSlugAsync(
                request.EntityType ?? string.Empty,
                request.Slug,
                storeResult.Payload,
                request.LanguageCode,
                request.ExcludedEntityId,
                cancellationToken);

            return this.Success(result, result.Success ? "SEO slug is valid." : "SEO slug is invalid.");
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StoreSeoSlugHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StoreSeoSlugHistoryDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StoreSeoSlugHistoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> History(
            [FromQuery] StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.StoreScopeFailure<IReadOnlyList<StoreSeoSlugHistoryDto>>(storeResult);
            }

            var history = await this.historyService.ListHistoryAsync(
                query.EntityType ?? string.Empty,
                query.EntityId,
                storeResult.Payload,
                query.LanguageCode,
                cancellationToken);

            return this.Success(history, "SEO slug history retrieved.");
        }

        private IActionResult StoreScopeFailure<TPayload>(CommerceStoreOperationResult<Guid> storeResult)
        {
            var statusCode = storeResult.Failure switch
            {
                CommerceStoreOperationFailure.NotFound => StatusCodes.Status404NotFound,
                CommerceStoreOperationFailure.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };

            return this.StatusCode(
                statusCode,
                CommerceNodeApiResponse<TPayload>.Failed(storeResult.Message));
        }
    }
}
