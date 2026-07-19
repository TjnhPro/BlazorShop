namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.ControlPlane.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/nodes")]
    [Authorize(Policy = ControlPlanePolicyNames.NodesRead)]
    public sealed class ControlPlaneNodesController : ControllerBase
    {
        private readonly IControlPlaneNodeService nodeService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneNodesController(
            IControlPlaneNodeService nodeService,
            IControlPlaneAuditService auditService)
        {
            this.nodeService = nodeService;
            this.auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var response = await this.nodeService.ListAsync(
                new ControlPlaneNodeListQuery(search, status, pageNumber, pageSize),
                cancellationToken);

            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                response,
                "Nodes loaded.");
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.nodeService.GetByPublicIdAsync(publicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> Create(CreateControlPlaneNodeRequest request, CancellationToken cancellationToken)
        {
            var result = await this.nodeService.CreateAsync(request, cancellationToken);

            if (result.Success && result.Payload is not null)
            {
                await this.WriteNodeAuditAsync("nodes.create", "success", result.Payload, cancellationToken);
                return CreatedAtAction(
                    nameof(Get),
                    new { publicId = result.Payload.PublicId },
                    ControlPlaneApiResponse<ControlPlaneNodeDetail>.Succeeded(result.Payload, "Node created."));
            }

            await this.WriteNodeAuditAsync("nodes.create", "failure", result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPut("{publicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> Update(Guid publicId, UpdateControlPlaneNodeRequest request, CancellationToken cancellationToken)
        {
            var result = await this.nodeService.UpdateAsync(publicId, request, cancellationToken);

            if (result.Success && result.Payload is not null)
            {
                await this.WriteNodeAuditAsync("nodes.update", "success", result.Payload, cancellationToken);
            }

            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/disable")]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> Disable(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.nodeService.DisableAsync(publicId, cancellationToken);

            if (result.Success && result.Payload is not null)
            {
                await this.WriteNodeAuditAsync("nodes.disable", "success", result.Payload, cancellationToken);
            }

            return ToActionResult(result);
        }

        private IActionResult ToActionResult(ApplicationResult<ControlPlaneNodeDetail> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Node loaded." : result.Message);
            }

            return result.Failure switch
            {
                ApplicationErrorKind.NotFound => ControlPlaneApiResponseWriter.Failure<ControlPlaneNodeDetail>(StatusCodes.Status404NotFound, result.Message),
                ApplicationErrorKind.Conflict => ControlPlaneApiResponseWriter.Failure<ControlPlaneNodeDetail>(StatusCodes.Status409Conflict, result.Message),
                ApplicationErrorKind.Validation => ControlPlaneApiResponseWriter.Failure<ControlPlaneNodeDetail>(StatusCodes.Status400BadRequest, result.Message),
                _ => ControlPlaneApiResponseWriter.Failure<ControlPlaneNodeDetail>(StatusCodes.Status400BadRequest, result.Message)
            };
        }

        private async Task WriteNodeAuditAsync(
            string action,
            string result,
            ControlPlaneNodeDetail? node,
            CancellationToken cancellationToken)
        {
            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "commerce_node",
                    Result: result,
                    ActorIdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                    ActorEmail: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                    EntityPublicId: node?.PublicId.ToString(),
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }
    }
}
