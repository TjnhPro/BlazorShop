extern alias ControlPlaneApi;

namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Infrastructure.Data.ControlPlane;
    using ControlPlaneApi::BlazorShop.ControlPlane.API.Authorization;
    using ControlPlaneApi::BlazorShop.ControlPlane.API.Controllers;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneAuthorizationTests
    {
        [Fact]
        public void PolicyMap_CoversAllSeededControlPlanePermissions()
        {
            Assert.Equal(
                ControlPlanePermissions.All.Order(StringComparer.Ordinal),
                ControlPlanePolicyNames.PermissionByPolicy.Values.Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task PermissionHandler_SucceedsForActiveProfileWithPermission()
        {
            await using var context = CreateContext();
            SeedProfile(context, status: "active", includePermission: true);
            var requirement = new ControlPlanePermissionRequirement(ControlPlanePermissions.NodesRead);
            var authorizationContext = CreateAuthorizationContext(requirement);
            var handler = new ControlPlanePermissionAuthorizationHandler(context);

            await handler.HandleAsync(authorizationContext);

            Assert.True(authorizationContext.HasSucceeded);
        }

        [Fact]
        public async Task PermissionHandler_DoesNotSucceedForDisabledProfile()
        {
            await using var context = CreateContext();
            SeedProfile(context, status: "disabled", includePermission: true);
            var requirement = new ControlPlanePermissionRequirement(ControlPlanePermissions.NodesRead);
            var authorizationContext = CreateAuthorizationContext(requirement);
            var handler = new ControlPlanePermissionAuthorizationHandler(context);

            await handler.HandleAsync(authorizationContext);

            Assert.False(authorizationContext.HasSucceeded);
        }

        [Fact]
        public void ControlPlaneControllerActions_HaveExplicitAuthBoundary()
        {
            var missingAuthMetadata = typeof(ControlPlaneAuthController).Assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
                .SelectMany(
                    controller => controller.GetMethods()
                        .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Length > 0)
                        .Where(method => !HasAuthMetadata(controller, method))
                        .Select(method => $"{controller.Name}.{method.Name}"))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(missingAuthMetadata);
        }

        [Fact]
        public void ControlPlaneAuthController_ProtectsSessionEndpoints()
        {
            Assert.True(MethodHasAttribute<AllowAnonymousAttribute>(nameof(ControlPlaneAuthController.Login)));
            Assert.True(MethodHasAttribute<AllowAnonymousAttribute>(nameof(ControlPlaneAuthController.RefreshToken)));
            Assert.True(MethodHasAttribute<AuthorizeAttribute>(nameof(ControlPlaneAuthController.Logout)));
            Assert.True(MethodHasAttribute<AuthorizeAttribute>(nameof(ControlPlaneAuthController.Me)));
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-auth-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }

        private static void SeedProfile(ControlPlaneDbContext context, string status, bool includePermission)
        {
            var permission = new ControlPlanePermission
            {
                Id = 100,
                Key = ControlPlanePermissions.NodesRead,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var role = new ControlPlaneRole
            {
                Id = 100,
                Key = "test_role",
                Name = "Test Role",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            if (includePermission)
            {
                role.Permissions.Add(new ControlPlaneRolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    Role = role,
                    Permission = permission,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            var adminUser = new ControlPlaneAdminUser
            {
                Id = 100,
                IdentityUserId = "identity-user-1",
                Email = "admin@example.com",
                DisplayName = "Admin",
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            adminUser.Roles.Add(new ControlPlaneAdminUserRole
            {
                AdminUserId = adminUser.Id,
                RoleId = role.Id,
                AdminUser = adminUser,
                Role = role,
                CreatedAt = DateTimeOffset.UtcNow
            });

            context.Permissions.Add(permission);
            context.Roles.Add(role);
            context.AdminUsers.Add(adminUser);
            context.SaveChanges();
        }

        private static AuthorizationHandlerContext CreateAuthorizationContext(ControlPlanePermissionRequirement requirement)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "identity-user-1")],
                authenticationType: "Test");

            return new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), resource: null);
        }

        private static bool HasAuthMetadata(Type controller, System.Reflection.MethodInfo method)
        {
            return controller.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Length > 0
                   || controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0
                   || method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Length > 0
                   || method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0;
        }

        private static bool MethodHasAttribute<TAttribute>(string methodName)
            where TAttribute : Attribute
        {
            var method = typeof(ControlPlaneAuthController).GetMethod(methodName);
            Assert.NotNull(method);

            return method!.GetCustomAttributes(typeof(TAttribute), inherit: true).Length > 0;
        }
    }
}
