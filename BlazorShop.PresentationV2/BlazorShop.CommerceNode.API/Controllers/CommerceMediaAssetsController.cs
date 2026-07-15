namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.DTOs;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/media/assets")]
    public sealed class CommerceMediaAssetsController : CommerceAdminControllerBase
    {
        private readonly ICommerceMediaAssetService mediaAssetService;

        public CommerceMediaAssetsController(ICommerceMediaAssetService mediaAssetService)
        {
            this.mediaAssetService = mediaAssetService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null,
            [FromQuery] string? usageType = null,
            CancellationToken cancellationToken = default)
        {
            var result = await this.mediaAssetService.ListAsync(
                new CommerceMediaAssetListQuery(pageNumber, pageSize, search, usageType),
                cancellationToken);
            return this.FromMediaAssetResult(result);
        }

        [HttpGet("{assetPublicId:guid}")]
        public async Task<IActionResult> Get(
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.mediaAssetService.GetAsync(assetPublicId, cancellationToken);
            return this.FromMediaAssetResult(result);
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null)
            {
                return this.Failure<CommerceMediaAssetDto>(
                    ServiceResponseType.ValidationError,
                    "Image file is required.");
            }

            await using var stream = file.OpenReadStream();
            var result = await this.mediaAssetService.UploadAsync(
                new CommerceMediaAssetUploadRequest(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken);
            return this.FromMediaAssetResult(result);
        }

        [HttpPut("{assetPublicId:guid}")]
        public async Task<IActionResult> UpdateMetadata(
            Guid assetPublicId,
            [FromBody] CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.mediaAssetService.UpdateMetadataAsync(assetPublicId, request, cancellationToken);
            return this.FromMediaAssetResult(result);
        }

        [HttpPost("{assetPublicId:guid}/replace")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Replace(
            Guid assetPublicId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null)
            {
                return this.Failure<CommerceMediaAssetDto>(
                    ServiceResponseType.ValidationError,
                    "Image file is required.");
            }

            await using var stream = file.OpenReadStream();
            var result = await this.mediaAssetService.ReplaceAsync(
                assetPublicId,
                new CommerceMediaAssetUploadRequest(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken);
            return this.FromMediaAssetResult(result);
        }

        [HttpDelete("{assetPublicId:guid}")]
        public async Task<IActionResult> Delete(
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.mediaAssetService.DeleteAsync(assetPublicId, cancellationToken);
            return this.FromMediaAssetResult(result);
        }

        private IActionResult FromMediaAssetResult<TPayload>(CommerceMediaAssetOperationResult<TPayload> result)
        {
            if (result.Success)
            {
                return this.Success(result.Payload, result.Message ?? "Media asset request completed.");
            }

            return this.Failure<TPayload>(
                ToServiceResponseType(result.Failure),
                result.Message ?? "Media asset request could not be completed.",
                result.Payload);
        }

        private static ServiceResponseType ToServiceResponseType(CommerceMediaAssetOperationFailure? failure)
        {
            return failure switch
            {
                CommerceMediaAssetOperationFailure.Validation => ServiceResponseType.ValidationError,
                CommerceMediaAssetOperationFailure.NotFound => ServiceResponseType.NotFound,
                CommerceMediaAssetOperationFailure.Conflict => ServiceResponseType.Conflict,
                _ => ServiceResponseType.Failure,
            };
        }
    }
}
