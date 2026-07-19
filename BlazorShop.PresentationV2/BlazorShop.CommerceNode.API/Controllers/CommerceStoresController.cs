namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/stores")]
    public sealed class CommerceStoresController : CommerceAdminControllerBase
    {
        private readonly ICommerceStoreService storeService;

        public CommerceStoresController(ICommerceStoreService storeService)
        {
            this.storeService = storeService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var result = await this.storeService.ListAsync(
                new CommerceStoreListQuery(status, skip, take),
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.GetByPublicIdAsync(publicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCommerceStoreRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.storeService.CreateAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPut("{publicId:guid}")]
        public async Task<IActionResult> Update(
            Guid publicId,
            [FromBody] UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.storeService.UpdateAsync(publicId, request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/activate")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<CommerceStoreDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<CommerceStoreDetail>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.SetStatusAsync(publicId, "active", cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/deactivate")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<CommerceStoreDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<CommerceStoreDetail>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.SetStatusAsync(publicId, "disabled", cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/archive")]
        public async Task<IActionResult> Archive(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.ArchiveAsync(publicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains")]
        public async Task<IActionResult> AddDomain(
            Guid publicId,
            [FromBody] CreateCommerceStoreDomainRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.storeService.AddDomainAsync(publicId, request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains/{domainId:guid}/verify")]
        public async Task<IActionResult> VerifyDomain(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken)
        {
            var result = await this.storeService.VerifyDomainAsync(publicId, domainId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains/{domainId:guid}/disable")]
        public async Task<IActionResult> DisableDomain(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken)
        {
            var result = await this.storeService.DisableDomainAsync(publicId, domainId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains/{domainId:guid}/primary")]
        public async Task<IActionResult> SetPrimaryDomain(
            Guid publicId,
            Guid domainId,
            CancellationToken cancellationToken)
        {
            var result = await this.storeService.SetPrimaryDomainAsync(publicId, domainId, cancellationToken);
            return ToActionResult(result);
        }

        private static IActionResult ToActionResult<TPayload>(ApplicationResult<TPayload> result)
        {
            return result.ToCommerceNodeActionResult();
        }
    }
}
