namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Health;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.ControlPlane.API.Responses;

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
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                await this.healthService.ListAsync(new ControlPlaneHealthListQuery(search, status, pageNumber, pageSize), cancellationToken),
                "Node health loaded.");
        }

        [HttpGet("nodes/{nodePublicId:guid}")]
        public async Task<IActionResult> Get(Guid nodePublicId, CancellationToken cancellationToken)
        {
            var result = await this.healthService.GetDetailAsync(nodePublicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("nodes/{nodePublicId:guid}/timeline")]
        public async Task<IActionResult> GetTimeline(
            Guid nodePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await this.healthService.GetTimelineAsync(
                nodePublicId,
                new ControlPlaneHealthTimelineQuery(pageNumber, pageSize),
                cancellationToken);
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

        private IActionResult ToActionResult<TPayload>(ApplicationResult<TPayload> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Health request completed." : result.Message);
            }

            return result.Failure switch
            {
                ApplicationErrorKind.NotFound => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status404NotFound, result.Message),
                ApplicationErrorKind.Validation => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message),
                _ => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message)
            };
        }
    }
}
