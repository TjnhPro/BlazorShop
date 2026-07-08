namespace BlazorShop.ControlPlane.API.Authorization
{
    using System.Security.Claims;

    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlanePermissionAuthorizationHandler
        : AuthorizationHandler<ControlPlanePermissionRequirement>
    {
        private readonly ControlPlaneDbContext dbContext;

        public ControlPlanePermissionAuthorizationHandler(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ControlPlanePermissionRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var identityUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? context.User.FindFirstValue("nameid")
                                 ?? context.User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return;
            }

            var hasPermission = await this.dbContext.AdminUsers
                .AsNoTracking()
                .Where(adminUser =>
                    adminUser.IdentityUserId == identityUserId
                    && adminUser.Status == "active"
                    && adminUser.DeletedAt == null)
                .SelectMany(adminUser => adminUser.Roles)
                .SelectMany(userRole => userRole.Role!.Permissions)
                .AnyAsync(rolePermission => rolePermission.Permission!.Key == requirement.Permission);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
