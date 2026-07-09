namespace BlazorShop.ControlPlane.API.Controllers
{
    using BlazorShop.Application.ControlPlane.Dashboard;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.ControlPlane.API.Responses;

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
        public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        {
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                await this.dashboardService.GetSummaryAsync(cancellationToken),
                "Dashboard summary loaded.");
        }
    }
}
