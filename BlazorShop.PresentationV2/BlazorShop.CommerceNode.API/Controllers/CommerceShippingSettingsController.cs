namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/shipping/settings")]
    public sealed class CommerceShippingSettingsController : CommerceAdminControllerBase
    {
        private readonly IStoreShippingSettingsService settingsService;

        public CommerceShippingSettingsController(IStoreShippingSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var result = await this.settingsService.GetAsync(cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromBody] UpdateStoreShippingSettingsRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.settingsService.UpdateAsync(request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
