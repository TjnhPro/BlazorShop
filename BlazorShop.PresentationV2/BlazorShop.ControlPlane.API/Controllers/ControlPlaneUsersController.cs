namespace BlazorShop.ControlPlane.API.Controllers
{
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

        public ControlPlaneUsersController(IControlPlaneUserManagementService userManagementService)
        {
            this.userManagementService = userManagementService;
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

            var body = new { message = result.Message };
            return result.Failure switch
            {
                ControlPlaneUserOperationFailure.NotFound => NotFound(body),
                ControlPlaneUserOperationFailure.Conflict => Conflict(body),
                ControlPlaneUserOperationFailure.Validation => BadRequest(body),
                _ => BadRequest(body)
            };
        }
    }
}
