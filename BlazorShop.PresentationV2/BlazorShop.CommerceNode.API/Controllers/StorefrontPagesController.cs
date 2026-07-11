namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.StorefrontPages;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/internal/pages")]
    public sealed class StorefrontPagesController : StorefrontInternalControllerBase
    {
        private readonly IStorefrontPageService storefrontPageService;

        public StorefrontPagesController(IStorefrontPageService storefrontPageService)
        {
            this.storefrontPageService = storefrontPageService;
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.GetPublishedBySlugAsync(slug, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
