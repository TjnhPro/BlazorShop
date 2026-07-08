namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.ControlPlane.Users;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/users")]
    [Authorize(Policy = ControlPlanePolicyNames.UsersRead)]
    public sealed class ControlPlaneUsersController : ControllerBase
    {
        private readonly IControlPlaneUserManagementService userManagementService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneUsersController(
            IControlPlaneUserManagementService userManagementService,
            IControlPlaneAuditService auditService)
        {
            this.userManagementService = userManagementService;
            this.auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<ControlPlaneUserListResponse>> List(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? roleKey,
            [FromQuery] string? permissionKey,
            [FromQuery] string? cursor,
            [FromQuery] int limit = 25,
            CancellationToken cancellationToken = default)
        {
            var response = await this.userManagementService.ListAsync(
                new ControlPlaneUserListQuery(search, status, roleKey, permissionKey, cursor, limit),
                cancellationToken);

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Policy = ControlPlanePolicyNames.UsersWrite)]
        public async Task<IActionResult> Create(CreateControlPlaneUserRequest request, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.CreateAsync(request, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.create",
                result.Success ? "success" : "failure",
                result.Payload?.User.PublicId,
                cancellationToken);

            if (result.Success && result.Payload is not null)
            {
                return CreatedAtAction(nameof(Get), new { publicId = result.Payload.User.PublicId }, result.Payload);
            }

            return ToFailureActionResult(result);
        }

        [HttpPut("{publicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.UsersWrite)]
        public async Task<IActionResult> Update(Guid publicId, UpdateControlPlaneUserRequest request, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.UpdateAsync(publicId, request, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.update",
                result.Success ? "success" : "failure",
                publicId,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/disable")]
        [Authorize(Policy = ControlPlanePolicyNames.UsersWrite)]
        public async Task<IActionResult> Disable(Guid publicId, ChangeControlPlaneUserStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.DisableAsync(publicId, request, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.disable",
                result.Success ? "success" : "failure",
                publicId,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/enable")]
        [Authorize(Policy = ControlPlanePolicyNames.UsersWrite)]
        public async Task<IActionResult> Enable(Guid publicId, ChangeControlPlaneUserStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.EnableAsync(publicId, request, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.enable",
                result.Success ? "success" : "failure",
                publicId,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.GetAsync(publicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("roles")]
        public async Task<ActionResult<ControlPlaneRoleCatalogResponse>> ListRoles(CancellationToken cancellationToken)
        {
            var response = await this.userManagementService.ListRolesAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<ControlPlanePermissionCatalogResponse>> ListPermissions(CancellationToken cancellationToken)
        {
            var response = await this.userManagementService.ListPermissionsAsync(cancellationToken);
            return Ok(response);
        }

        private IActionResult ToActionResult(ControlPlaneUserOperationResult<ControlPlaneUserDetail> result)
        {
            if (result.Success)
            {
                return Ok(result.Payload);
            }

            return ToFailureActionResult(result);
        }

        private IActionResult ToFailureActionResult<TPayload>(ControlPlaneUserOperationResult<TPayload> result)
        {
            var body = new { message = result.Message };
            return result.Failure switch
            {
                ControlPlaneUserOperationFailure.NotFound => NotFound(body),
                ControlPlaneUserOperationFailure.Conflict => Conflict(body),
                ControlPlaneUserOperationFailure.Validation => BadRequest(body),
                _ => BadRequest(body)
            };
        }

        private ControlPlaneUserActor GetActor()
        {
            return new ControlPlaneUserActor(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"));
        }

        private async Task WriteUserAuditAsync(
            string action,
            string result,
            Guid? entityPublicId,
            CancellationToken cancellationToken)
        {
            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "control_plane_admin_user",
                    Result: result,
                    ActorIdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                    ActorEmail: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                    EntityPublicId: entityPublicId?.ToString(),
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }
    }
}
