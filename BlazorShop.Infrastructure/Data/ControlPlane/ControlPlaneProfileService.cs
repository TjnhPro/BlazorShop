namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneProfileService : IControlPlaneProfileService
    {
        private const long PlatformOwnerRoleId = 1;

        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneProfileService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ControlPlaneProfileResult> EnsureProfileAsync(
            string identityUserId,
            string email,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var profile = await this.dbContext.AdminUsers
                .Include(adminUser => adminUser.Roles)
                .FirstOrDefaultAsync(adminUser => adminUser.IdentityUserId == identityUserId, cancellationToken);

            if (profile is not null)
            {
                profile.Email = normalizedEmail;
                profile.DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedEmail : displayName.Trim();
                if (profile.Status == "active")
                {
                    profile.LastLoginAt = DateTimeOffset.UtcNow;
                }

                profile.UpdatedAt = DateTimeOffset.UtcNow;

                await this.dbContext.SaveChangesAsync(cancellationToken);

                return new ControlPlaneProfileResult(profile.Id, profile.IdentityUserId, profile.Email, profile.DisplayName, profile.Status, Created: false);
            }

            var isFirstAdminProfile = !await this.dbContext.AdminUsers.AnyAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            profile = new ControlPlaneAdminUser
            {
                IdentityUserId = identityUserId,
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedEmail : displayName.Trim(),
                Status = "active",
                LastLoginAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (isFirstAdminProfile)
            {
                profile.Roles.Add(new ControlPlaneAdminUserRole
                {
                    RoleId = PlatformOwnerRoleId,
                    CreatedAt = now
                });
            }

            this.dbContext.AdminUsers.Add(profile);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return new ControlPlaneProfileResult(profile.Id, profile.IdentityUserId, profile.Email, profile.DisplayName, profile.Status, Created: true);
        }
    }
}
