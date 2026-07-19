namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/products/{productId:guid}/media")]
    public sealed class CommerceProductMediaController : CommerceAdminControllerBase
    {
        private readonly IProductMediaService productMediaService;

        public CommerceProductMediaController(IProductMediaService productMediaService)
        {
            this.productMediaService = productMediaService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await this.productMediaService.ListAsync(
                productId,
                new ProductMediaListQuery(pageNumber, pageSize),
                cancellationToken);
            return this.FromProductMediaResult(result);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(
            Guid productId,
            [FromBody] ImportProductMediaRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.productMediaService.ImportAsync(
                productId,
                request,
                this.User.Identity?.Name,
                this.HttpContext.TraceIdentifier,
                cancellationToken);
            return this.FromProductMediaResult(result);
        }

        [HttpPut("order")]
        public async Task<IActionResult> UpdateOrder(
            Guid productId,
            [FromBody] UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.productMediaService.UpdateOrderAsync(productId, request, cancellationToken);
            return this.FromProductMediaResult(result);
        }

        [HttpPost("{mediaPublicId:guid}/primary")]
        public async Task<IActionResult> SetPrimary(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            var result = await this.productMediaService.SetPrimaryAsync(productId, mediaPublicId, cancellationToken);
            return this.FromProductMediaResult(result);
        }

        [HttpDelete("{mediaPublicId:guid}")]
        public async Task<IActionResult> Delete(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            var result = await this.productMediaService.DeleteAsync(productId, mediaPublicId, cancellationToken);
            return this.FromProductMediaResult(result);
        }

        [HttpPost("{mediaPublicId:guid}/retry")]
        public async Task<IActionResult> Retry(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken)
        {
            var result = await this.productMediaService.RetryAsync(
                productId,
                mediaPublicId,
                this.User.Identity?.Name,
                this.HttpContext.TraceIdentifier,
                cancellationToken);
            return this.FromProductMediaResult(result);
        }

        private IActionResult FromProductMediaResult<TPayload>(ApplicationResult<TPayload> result)
        {
            return result.ToCommerceNodeActionResult();
        }
    }
}
