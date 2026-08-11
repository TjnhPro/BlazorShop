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
        private readonly ICommerceBrandingAssetService brandingAssetService;

        public CommerceMediaAssetsController(
            ICommerceMediaAssetService mediaAssetService,
            ICommerceBrandingAssetService brandingAssetService)
        {
            this.mediaAssetService = mediaAssetService;
            this.brandingAssetService = brandingAssetService;
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

        [HttpPost("branding/{slot}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadBranding(
            string slot,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null)
            {
                return ApplicationResult<CommerceBrandingAssetResponse>
                    .Failed(ApplicationError.Validation("branding.validation", "Image file is required."))
                    .ToCommerceNodeActionResult();
            }

            await using var stream = file.OpenReadStream();
            var result = await this.brandingAssetService.UploadAsync(
                new CommerceBrandingAssetUploadRequest(
                    slot,
                    new CommerceMediaAssetUploadRequest(stream, file.FileName, file.ContentType, file.Length)),
                cancellationToken);
            return result.ToCommerceNodeActionResult();
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
