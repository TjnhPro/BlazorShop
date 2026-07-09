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

        public async Task SeedConfiguredAccountsAsync(CancellationToken cancellationToken = default)
        {
            await this.SeedAccountAsync(
                "SeedAdmin",
                "Admin",
                "platform_owner",
                "Control Plane development admin seed failed",
                cancellationToken);

            await this.SeedAccountAsync(
                "SeedUser",
                "User",
                "auditor",
                "Control Plane development user seed failed",
                cancellationToken);
        }

        public Task SeedPlatformOwnerAsync(CancellationToken cancellationToken = default)
        {
            return this.SeedAccountAsync(
                "SeedAdmin",
                "Admin",
                "platform_owner",
                "Control Plane development admin seed failed",
                cancellationToken);
        }

        private async Task SeedAccountAsync(
            string sectionName,
            string defaultIdentityRoleName,
            string defaultControlPlaneRoleKey,
            string errorPrefix,
            CancellationToken cancellationToken)
        {
            var sectionPath = $"ControlPlane:{sectionName}";
            if (!this.configuration.GetValue($"{sectionPath}:Enabled", false))
            {
                return;
            }

            var email = this.configuration[$"{sectionPath}:Email"];
            var password = this.configuration[$"{sectionPath}:Password"];
            var displayName = this.configuration[$"{sectionPath}:DisplayName"];
            var identityRoleName = this.configuration[$"{sectionPath}:IdentityRole"];
            var controlPlaneRoleKey = this.configuration[$"{sectionPath}:ControlPlaneRoleKey"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            displayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
            identityRoleName = string.IsNullOrWhiteSpace(identityRoleName) ? defaultIdentityRoleName : identityRoleName;
            controlPlaneRoleKey = string.IsNullOrWhiteSpace(controlPlaneRoleKey) ? defaultControlPlaneRoleKey : controlPlaneRoleKey;

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
                    throw new InvalidOperationException($"{errorPrefix}: {errors}");
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

            await this.EnsureControlPlaneProfileAsync(user, displayName, controlPlaneRoleKey, cancellationToken);
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

        private async Task EnsureControlPlaneProfileAsync(
            AppUser user,
            string displayName,
            string controlPlaneRoleKey,
            CancellationToken cancellationToken)
        {
            var controlPlaneRole = await this.dbContext.ControlPlaneRoles
                .FirstOrDefaultAsync(role => role.Key == controlPlaneRoleKey, cancellationToken);

            if (controlPlaneRole is null)
            {
                throw new InvalidOperationException($"Control Plane role '{controlPlaneRoleKey}' is not seeded.");
            }

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
                        RoleId = controlPlaneRole.Id,
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

            if (profile.Roles.All(role => role.RoleId != controlPlaneRole.Id))
            {
                profile.Roles.Add(
                    new ControlPlaneAdminUserRole
                    {
                        AdminUserId = profile.Id,
                        RoleId = controlPlaneRole.Id,
                        CreatedAt = now
                    });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
