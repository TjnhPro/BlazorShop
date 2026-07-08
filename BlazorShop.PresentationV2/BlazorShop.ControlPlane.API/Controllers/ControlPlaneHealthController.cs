namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Health;
    using BlazorShop.Application.ControlPlane.Security;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/health")]
    [Authorize(Policy = ControlPlanePolicyNames.HealthRead)]
    public sealed class ControlPlaneHealthController : ControllerBase
    {
        private readonly IControlPlaneHealthService healthService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneHealthController(
            IControlPlaneHealthService healthService,
            IControlPlaneAuditService auditService)
        {
            this.healthService = healthService;
            this.auditService = auditService;
        }

        [HttpGet("nodes")]
        public async Task<ActionResult<ControlPlaneHealthListResponse>> List(CancellationToken cancellationToken)
        {
            return Ok(await this.healthService.ListAsync(cancellationToken));
        }

        [HttpGet("nodes/{nodePublicId:guid}")]
        public async Task<IActionResult> Get(Guid nodePublicId, CancellationToken cancellationToken)
        {
            var result = await this.healthService.GetDetailAsync(nodePublicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("nodes/{nodePublicId:guid}/probe")]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> Probe(Guid nodePublicId, CancellationToken cancellationToken)
        {
            var result = await this.healthService.ProbeAsync(nodePublicId, cancellationToken);

            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: "health.probe",
                    EntityType: "commerce_node",
                    Result: result.Success ? "success" : "failure",
                    ActorIdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                    ActorEmail: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                    EntityPublicId: nodePublicId.ToString(),
                    MetadataJson: $$"""{"nodePublicId":"{{nodePublicId}}"}""",
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);

            return ToActionResult(result);
        }

        private IActionResult ToActionResult<TPayload>(ControlPlaneHealthOperationResult<TPayload> result)
        {
            if (result.Success)
            {
                return Ok(result.Payload);
            }

            var body = new { message = result.Message };
            return result.Failure switch
            {
                ControlPlaneHealthOperationFailure.NotFound => NotFound(body),
                ControlPlaneHealthOperationFailure.Validation => BadRequest(body),
                _ => BadRequest(body)
            };
        }
    }
}
