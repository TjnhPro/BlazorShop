namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/navigation")]
    public sealed class CommerceNavigationController : CommerceAdminControllerBase
    {
        private readonly IStoreNavigationService navigationService;

        public CommerceNavigationController(IStoreNavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        [HttpGet("menus")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StoreNavigationMenuSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StoreNavigationMenuSummaryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListMenus(CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.ListMenusAsync(cancellationToken));
        }

        [HttpPost("menus")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateMenu(
            [FromBody] CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.CreateMenuAsync(request, cancellationToken));
        }

        [HttpGet("menus/{menuPublicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMenu(Guid menuPublicId, CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.GetMenuAsync(menuPublicId, cancellationToken));
        }

        [HttpPut("menus/{menuPublicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMenu(
            Guid menuPublicId,
            [FromBody] UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.UpdateMenuAsync(menuPublicId, request, cancellationToken));
        }

        [HttpPost("menus/{menuPublicId:guid}/items")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateItem(
            Guid menuPublicId,
            [FromBody] CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.CreateItemAsync(menuPublicId, request, cancellationToken));
        }

        [HttpPut("items/{itemPublicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateItem(
            Guid itemPublicId,
            [FromBody] UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.UpdateItemAsync(itemPublicId, request, cancellationToken));
        }

        [HttpDelete("items/{itemPublicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ArchiveItem(Guid itemPublicId, CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.ArchiveItemAsync(itemPublicId, cancellationToken));
        }

        [HttpPut("menus/{menuPublicId:guid}/items/order")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateItemOrder(
            Guid menuPublicId,
            [FromBody] UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken)
        {
            return this.FromServiceResponse(await this.navigationService.UpdateItemOrderAsync(menuPublicId, request, cancellationToken));
        }

        [HttpGet("system-targets")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<StoreNavigationTargetOptionDto>>), StatusCodes.Status200OK)]
        public IActionResult ListSystemTargets()
        {
            return this.Success(this.navigationService.ListSystemTargets(), "Navigation system targets retrieved.");
        }
    }

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
