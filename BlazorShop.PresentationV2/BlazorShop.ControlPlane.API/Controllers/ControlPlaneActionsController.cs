namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Actions;
    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.ControlPlane.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/actions")]
    [Authorize(Policy = ControlPlanePolicyNames.ActionsRead)]
    public sealed class ControlPlaneActionsController : ControllerBase
    {
        private readonly IControlPlaneActionService actionService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneActionsController(
            IControlPlaneActionService actionService,
            IControlPlaneAuditService auditService)
        {
            this.actionService = actionService;
            this.auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] string? actionType,
            [FromQuery] Guid? nodePublicId,
            [FromQuery] Guid? storePublicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                await this.actionService.ListAsync(
                    new ControlPlaneActionListQuery(status, actionType, nodePublicId, storePublicId, pageNumber, pageSize),
                    cancellationToken),
                "Control actions loaded.");
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.actionService.GetByPublicIdAsync(publicId, cancellationToken));
        }

        [HttpPost]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> Enqueue(EnqueueControlActionRequest request, CancellationToken cancellationToken)
        {
            var result = await this.actionService.EnqueueAsync(request, cancellationToken);
            await this.WriteActionAuditAsync("actions.enqueue", result, result.Payload, cancellationToken);

            return result.Success && result.Payload is not null && !result.AlreadyExists
                ? CreatedAtAction(
                    nameof(Get),
                    new { publicId = result.Payload.PublicId },
                    ControlPlaneApiResponse<ControlPlaneActionDetail>.Succeeded(result.Payload, "Control action enqueued."))
                : ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/attempts")]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> RecordAttempt(
            Guid publicId,
            RecordControlActionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.actionService.RecordAttemptAsync(publicId, request, cancellationToken);
            await this.WriteActionAuditAsync("actions.attempt.record", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/cancel")]
        [Authorize(Policy = ControlPlanePolicyNames.NodesWrite)]
        public async Task<IActionResult> Cancel(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.actionService.CancelAsync(publicId, cancellationToken);
            await this.WriteActionAuditAsync("actions.cancel", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        private IActionResult ToActionResult(ControlPlaneActionOperationResult<ControlPlaneActionDetail> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Control action request completed." : result.Message);
            }

            return result.Failure switch
            {
                ControlPlaneActionOperationFailure.NotFound => ControlPlaneApiResponseWriter.Failure<ControlPlaneActionDetail>(StatusCodes.Status404NotFound, result.Message),
                ControlPlaneActionOperationFailure.Conflict => ControlPlaneApiResponseWriter.Failure<ControlPlaneActionDetail>(StatusCodes.Status409Conflict, result.Message),
                ControlPlaneActionOperationFailure.Validation => ControlPlaneApiResponseWriter.Failure<ControlPlaneActionDetail>(StatusCodes.Status400BadRequest, result.Message),
                _ => ControlPlaneApiResponseWriter.Failure<ControlPlaneActionDetail>(StatusCodes.Status400BadRequest, result.Message)
            };
        }

        private async Task WriteActionAuditAsync(
            string action,
            ControlPlaneActionOperationResult<ControlPlaneActionDetail> result,
            ControlPlaneActionDetail? controlAction,
            CancellationToken cancellationToken)
        {
            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "control_action",
                    Result: result.Success ? "success" : "failure",
                    ActorIdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                    ActorEmail: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                    EntityPublicId: controlAction?.PublicId.ToString(),
                    MetadataJson: controlAction is null
                        ? null
                        : $$"""{"actionType":"{{controlAction.ActionType}}","nodePublicId":"{{controlAction.NodePublicId}}","correlationId":"{{controlAction.CorrelationId}}"}""",
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }
    }
}
