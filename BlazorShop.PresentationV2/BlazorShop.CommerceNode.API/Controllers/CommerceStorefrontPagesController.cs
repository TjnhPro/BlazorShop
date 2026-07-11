namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.StorefrontPages;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/pages")]
    public sealed class CommerceStorefrontPagesController : CommerceAdminControllerBase
    {
        private readonly IStorefrontPageService storefrontPageService;

        public CommerceStorefrontPagesController(IStorefrontPageService storefrontPageService)
        {
            this.storefrontPageService = storefrontPageService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] StorefrontPageListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.ListAsync(query, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateStorefrontPageRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.CreateAsync(request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.GetByIdAsync(id, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.UpdateAsync(id, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.ArchiveAsync(id, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
