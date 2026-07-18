namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/navigation")]
    public sealed class StorefrontScopedNavigationController : StorefrontApiControllerBase
    {
        private readonly IStoreNavigationService navigationService;

        public StorefrontScopedNavigationController(IStoreNavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        [HttpGet("{systemName}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationPublicMenuDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMenu(string systemName, CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.GetPublicMenuAsync(systemName, cancellationToken));
        }
    }
}
