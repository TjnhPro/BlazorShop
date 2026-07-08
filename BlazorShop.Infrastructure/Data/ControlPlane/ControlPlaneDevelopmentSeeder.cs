namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public sealed class ControlPlaneDevelopmentSeeder
    {
        private readonly ControlPlaneDbContext dbContext;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;

        public ControlPlaneDevelopmentSeeder(
            ControlPlaneDbContext dbContext,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
        }

        public async Task SeedPlatformOwnerAsync(CancellationToken cancellationToken = default)
        {
            if (!this.configuration.GetValue("ControlPlane:SeedAdmin:Enabled", false))
            {
                return;
            }

            var email = this.configuration["ControlPlane:SeedAdmin:Email"];
            var password = this.configuration["ControlPlane:SeedAdmin:Password"];
            var displayName = this.configuration["ControlPlane:SeedAdmin:DisplayName"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            displayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName;

            const string identityRoleName = "Admin";
            if (!await this.roleManager.RoleExistsAsync(identityRoleName))
            {
                await this.roleManager.CreateAsync(new IdentityRole(identityRoleName));
            }

            var user = await this.userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    FullName = displayName,
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    CreatedOn = DateTime.UtcNow
                };

                var createResult = await this.userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
                    throw new InvalidOperationException($"Control Plane development admin seed failed: {errors}");
                }
            }
            else
            {
                user.FullName = displayName;
                user.EmailConfirmed = true;
                user.LockoutEnabled = true;
                await this.userManager.UpdateAsync(user);
            }

            if (!await this.userManager.IsInRoleAsync(user, identityRoleName))
            {
                await this.userManager.AddToRoleAsync(user, identityRoleName);
            }

            await this.EnsurePlatformOwnerProfileAsync(user, displayName, cancellationToken);
        }

        public async Task SeedLocalMockNodeAsync(CancellationToken cancellationToken = default)
        {
            const string nodeKey = "local-mock-node";

            if (await this.dbContext.Nodes.AnyAsync(node => node.NodeKey == nodeKey, cancellationToken))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var node = new CommerceNode
            {
                NodeKey = nodeKey,
                Name = "Local Mock Node",
                Status = "unknown",
                Description = "Development-only placeholder node for Control Plane smoke testing.",
                CreatedAt = now,
                UpdatedAt = now,
                Endpoints =
                [
                    new CommerceNodeEndpoint
                    {
                        Kind = "control_api",
                        Url = "http://localhost:5180/api/controlpanel",
                        IsPrimary = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                ]
            };

            this.dbContext.Nodes.Add(node);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsurePlatformOwnerProfileAsync(AppUser user, string displayName, CancellationToken cancellationToken)
        {
            const long platformOwnerRoleId = 1;

            var now = DateTimeOffset.UtcNow;
            var profile = await this.dbContext.AdminUsers
                .Include(adminUser => adminUser.Roles)
                .FirstOrDefaultAsync(adminUser => adminUser.IdentityUserId == user.Id, cancellationToken);

            if (profile is null)
            {
                profile = new ControlPlaneAdminUser
                {
                    IdentityUserId = user.Id,
                    Email = user.Email!,
                    DisplayName = displayName,
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                profile.Roles.Add(
                    new ControlPlaneAdminUserRole
                    {
                        RoleId = platformOwnerRoleId,
                        CreatedAt = now
                    });

                this.dbContext.AdminUsers.Add(profile);
                await this.dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            profile.Email = user.Email!;
            profile.DisplayName = displayName;
            profile.Status = "active";
            profile.UpdatedAt = now;

            if (profile.Roles.All(role => role.RoleId != platformOwnerRoleId))
            {
                profile.Roles.Add(
                    new ControlPlaneAdminUserRole
                    {
                        AdminUserId = profile.Id,
                        RoleId = platformOwnerRoleId,
                        CreatedAt = now
                    });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
