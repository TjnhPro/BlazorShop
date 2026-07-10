namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.ProductImports;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/products/imports")]
    public sealed class CommerceProductImportsController : CommerceAdminControllerBase
    {
        private readonly IProductImportService productImportService;

        public CommerceProductImportsController(IProductImportService productImportService)
        {
            this.productImportService = productImportService;
        }

        [HttpPost("/api/commerce/admin/products/import")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromForm] string? mode,
            CancellationToken cancellationToken)
        {
            if (file is null)
            {
                return this.Failure<object>(Application.DTOs.ServiceResponseType.ValidationError, "A CSV file is required.");
            }

            await using var stream = file.OpenReadStream();
            var result = await this.productImportService.UploadAsync(
                new ProductImportUploadRequest(
                    file.FileName,
                    mode,
                    stream,
                    file.Length,
                    this.User.Identity?.Name),
                cancellationToken);

            return this.FromServiceResponse(result);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ProductImportJobListQuery query, CancellationToken cancellationToken)
        {
            var result = await this.productImportService.ListAsync(query, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{jobPublicId:guid}")]
        public async Task<IActionResult> Get(Guid jobPublicId, CancellationToken cancellationToken)
        {
            var result = await this.productImportService.GetByPublicIdAsync(jobPublicId, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{jobPublicId:guid}/rows")]
        public async Task<IActionResult> ListRows(
            Guid jobPublicId,
            [FromQuery] ProductImportRowsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await this.productImportService.ListRowsAsync(jobPublicId, query, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
