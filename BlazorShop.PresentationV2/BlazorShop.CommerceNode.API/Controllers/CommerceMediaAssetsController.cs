namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.CommerceNode.API.Responses;

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
                return ApplicationResult<CommerceMediaAssetDto>
                    .Failed(ApplicationError.Validation("media.validation", "Image file is required."))
                    .ToCommerceNodeActionResult();
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
                return ApplicationResult<CommerceMediaAssetDto>
                    .Failed(ApplicationError.Validation("media.validation", "Image file is required."))
                    .ToCommerceNodeActionResult();
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

        private IActionResult FromMediaAssetResult<TPayload>(ApplicationResult<TPayload> result)
        {
            return result.ToCommerceNodeActionResult();
        }
    }
}
