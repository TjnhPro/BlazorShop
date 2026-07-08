namespace BlazorShop.ControlPlane.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/system")]
    public sealed class SystemController : ControllerBase
    {
        [HttpGet("info")]
        public ActionResult<SystemInfoResponse> GetInfo()
        {
            return Ok(new SystemInfoResponse(
                Name: "BlazorShop Control Plane",
                Status: "starting",
                Version: typeof(SystemController).Assembly.GetName().Version?.ToString() ?? "0.0.0"));
        }
    }

    public sealed record SystemInfoResponse(string Name, string Status, string Version);
}
