namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/security-privacy")]
    public sealed class CommerceSecurityPrivacyController : CommerceAdminControllerBase
    {
        private readonly IStoreSecurityPrivacySettingsService settingsService;

        public CommerceSecurityPrivacyController(IStoreSecurityPrivacySettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var result = await this.settingsService.GetAsync(cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateStoreSecurityPrivacySettingsRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.settingsService.UpdateAsync(request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
