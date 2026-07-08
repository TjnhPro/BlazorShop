namespace BlazorShop.ControlPlane.API.Controllers
{
    using BlazorShop.Application.ControlPlane.Dashboard;
    using BlazorShop.Application.ControlPlane.Security;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/dashboard")]
    [Authorize(Policy = ControlPlanePolicyNames.NodesRead)]
    public sealed class ControlPlaneDashboardController : ControllerBase
    {
        private readonly IControlPlaneDashboardService dashboardService;

        public ControlPlaneDashboardController(IControlPlaneDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
        public async Task<ActionResult<ControlPlaneDashboardSummary>> Summary(CancellationToken cancellationToken)
        {
            return Ok(await this.dashboardService.GetSummaryAsync(cancellationToken));
        }
    }
}
