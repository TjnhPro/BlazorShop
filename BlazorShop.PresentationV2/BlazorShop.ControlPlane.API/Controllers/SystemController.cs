namespace BlazorShop.ControlPlane.API.Controllers
{
    using BlazorShop.ControlPlane.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/system")]
    [AllowAnonymous]
    public sealed class SystemController : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                new SystemInfoResponse(
                Name: "BlazorShop Control Plane",
                Status: "starting",
                    Version: typeof(SystemController).Assembly.GetName().Version?.ToString() ?? "0.0.0"),
                "Control Plane system info loaded.");
        }
    }

    public sealed record SystemInfoResponse(string Name, string Status, string Version);
}
