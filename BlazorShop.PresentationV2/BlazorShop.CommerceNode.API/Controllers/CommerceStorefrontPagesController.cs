namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/pages")]
    public sealed class CommerceStorefrontPagesController : CommerceAdminControllerBase
    {
        private readonly IStorefrontPageService storefrontPageService;
        private readonly IStorefrontPageTemplateService templateService;

        public CommerceStorefrontPagesController(
            IStorefrontPageService storefrontPageService,
            IStorefrontPageTemplateService templateService)
        {
            this.storefrontPageService = storefrontPageService;
            this.templateService = templateService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageListResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> List(
            [FromQuery] StorefrontPageListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.ListAsync(query, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("templates")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>), StatusCodes.Status200OK)]
        public IActionResult ListTemplates()
        {
            return this.Success(this.templateService.ListDefinitions(), "Storefront page templates retrieved.");
        }

        [HttpGet("template-status")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPageTemplateStatusDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPageTemplateStatusDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplateStatus(CancellationToken cancellationToken)
        {
            var result = await this.templateService.GetStatusAsync(cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("templates/{pageKey}/draft")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateDraftFromTemplate(
            string pageKey,
            [FromBody] CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.templateService.CreateDraftFromTemplateAsync(pageKey, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/template")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MapTemplate(
            Guid id,
            [FromBody] MapPageTemplateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.templateService.MapExistingPageAsync(id, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{id:guid}/template")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClearTemplate(Guid id, CancellationToken cancellationToken)
        {
            var result = await this.templateService.ClearPageKeyAsync(id, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/navigation")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StorefrontPageDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNavigation(
            Guid id,
            [FromBody] UpdatePageNavigationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.templateService.UpdateNavigationAsync(id, request, cancellationToken);
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
