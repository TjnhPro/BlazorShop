namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Features;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/features")]
    public sealed class CommerceFeatureStatesController : CommerceAdminControllerBase
    {
        private readonly IStoreFeatureStateService featureStateService;

        public CommerceFeatureStatesController(IStoreFeatureStateService featureStateService)
        {
            this.featureStateService = featureStateService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var features = await this.featureStateService.GetAsync(cancellationToken);
            return this.Success(features, "Feature states loaded.");
        }

        [HttpPut("{featureKey}")]
        public async Task<IActionResult> Update(
            string featureKey,
            [FromBody] UpdateStoreFeatureStateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.featureStateService.UpdateAsync(featureKey, request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}
