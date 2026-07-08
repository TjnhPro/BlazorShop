namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.ControlPlane.Users;
    using BlazorShop.ControlPlane.API.Responses;

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
        public async Task<IActionResult> List(
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

            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                response,
                "Users loaded.");
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
                return CreatedAtAction(
                    nameof(Get),
                    new { publicId = result.Payload.User.PublicId },
                    ControlPlaneApiResponse<CreateControlPlaneUserResponse>.Succeeded(result.Payload, "User created."));
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

        [HttpPost("{publicId:guid}/roles")]
        [Authorize(Policy = ControlPlanePolicyNames.RolesAssign)]
        public async Task<IActionResult> AssignRole(Guid publicId, AssignControlPlaneRoleRequest request, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.AssignRoleAsync(publicId, request, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.role.assign",
                result.Success ? "success" : "failure",
                publicId,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpDelete("{publicId:guid}/roles/{roleKey}")]
        [Authorize(Policy = ControlPlanePolicyNames.RolesAssign)]
        public async Task<IActionResult> RemoveRole(Guid publicId, string roleKey, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.RemoveRoleAsync(publicId, roleKey, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.role.remove",
                result.Success ? "success" : "failure",
                publicId,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/permissions")]
        [Authorize(Policy = ControlPlanePolicyNames.PermissionsManage)]
        public async Task<IActionResult> AssignPermission(Guid publicId, AssignControlPlanePermissionRequest request, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.AssignPermissionAsync(publicId, request, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.permission.assign",
                result.Success ? "success" : "failure",
                publicId,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpDelete("{publicId:guid}/permissions/{permissionKey}")]
        [Authorize(Policy = ControlPlanePolicyNames.PermissionsManage)]
        public async Task<IActionResult> RemovePermission(Guid publicId, string permissionKey, CancellationToken cancellationToken)
        {
            var result = await this.userManagementService.RemovePermissionAsync(publicId, permissionKey, GetActor(), cancellationToken);

            await this.WriteUserAuditAsync(
                "users.permission.remove",
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
        public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
        {
            var response = await this.userManagementService.ListRolesAsync(cancellationToken);
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                response,
                "Control Plane roles loaded.");
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> ListPermissions(CancellationToken cancellationToken)
        {
            var response = await this.userManagementService.ListPermissionsAsync(cancellationToken);
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                response,
                "Control Plane permissions loaded.");
        }

        private IActionResult ToActionResult(ControlPlaneUserOperationResult<ControlPlaneUserDetail> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "User request completed." : result.Message);
            }

            return ToFailureActionResult(result);
        }

        private IActionResult ToFailureActionResult<TPayload>(ControlPlaneUserOperationResult<TPayload> result)
        {
            return result.Failure switch
            {
                ControlPlaneUserOperationFailure.NotFound => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status404NotFound, result.Message),
                ControlPlaneUserOperationFailure.Conflict => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status409Conflict, result.Message),
                ControlPlaneUserOperationFailure.Validation => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message),
                _ => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message)
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
