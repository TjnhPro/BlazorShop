namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneDbContext : IdentityDbContext<AppUser>
    {
        private static readonly DateTimeOffset SeedCreatedAt = new(2026, 7, 8, 0, 0, 0, TimeSpan.Zero);

        public ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<ControlPlaneAdminUser> AdminUsers => Set<ControlPlaneAdminUser>();

        public DbSet<ControlPlaneRole> ControlPlaneRoles => Set<ControlPlaneRole>();

        public DbSet<ControlPlanePermission> Permissions => Set<ControlPlanePermission>();

        public DbSet<ControlPlaneAdminUserPermission> AdminUserPermissions => Set<ControlPlaneAdminUserPermission>();

        public DbSet<CommerceNode> Nodes => Set<CommerceNode>();

        public DbSet<CommerceNodeEndpoint> NodeEndpoints => Set<CommerceNodeEndpoint>();

        public DbSet<CommerceNodeCredential> NodeCredentials => Set<CommerceNodeCredential>();

        public DbSet<NodeHealthSnapshot> NodeHealthSnapshots => Set<NodeHealthSnapshot>();

        public DbSet<NodeCapabilitySnapshot> NodeCapabilitySnapshots => Set<NodeCapabilitySnapshot>();

        public DbSet<StoreRegistry> Stores => Set<StoreRegistry>();

        public DbSet<StoreDomainRegistry> StoreDomains => Set<StoreDomainRegistry>();

        public DbSet<ControlAction> Actions => Set<ControlAction>();

        public DbSet<ControlActionAttempt> ActionAttempts => Set<ControlActionAttempt>();

        public DbSet<ControlAuditLog> AuditLogs => Set<ControlAuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("pgcrypto");

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ControlPlaneDbContext).Assembly,
                type => type.Namespace == "BlazorShop.Infrastructure.Data.ControlPlane.Configurations");
            SeedAuthorization(modelBuilder);
        }

        private static void SeedAuthorization(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ControlPlaneRole>().HasData(
                new ControlPlaneRole { Id = 1, Key = "platform_owner", Name = "Platform Owner", Description = "Full Control Plane access.", IsSystem = true, CreatedAt = SeedCreatedAt, UpdatedAt = SeedCreatedAt },
                new ControlPlaneRole { Id = 2, Key = "node_operator", Name = "Node Operator", Description = "Manage nodes, stores, credentials, and health operations.", IsSystem = true, CreatedAt = SeedCreatedAt, UpdatedAt = SeedCreatedAt },
                new ControlPlaneRole { Id = 3, Key = "auditor", Name = "Auditor", Description = "Read-only audit and operations access.", IsSystem = true, CreatedAt = SeedCreatedAt, UpdatedAt = SeedCreatedAt });

            modelBuilder.Entity<ControlPlanePermission>().HasData(
                new ControlPlanePermission { Id = 1, Key = "nodes.read", Description = "Read node registry.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 2, Key = "nodes.write", Description = "Create, update, and disable nodes.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 3, Key = "credentials.rotate", Description = "Create, revoke, and rotate node credentials.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 4, Key = "stores.read", Description = "Read store registry metadata.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 5, Key = "stores.write", Description = "Create, update, archive, and assign stores.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 6, Key = "health.read", Description = "Read node health and capability snapshots.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 7, Key = "actions.read", Description = "Read control action state and attempts.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 8, Key = "audit.read", Description = "Read audit logs.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 9, Key = "users.read", Description = "List and view Control Plane users.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 10, Key = "users.write", Description = "Create, update, enable, and disable Control Plane users.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 11, Key = "roles.assign", Description = "Assign and remove Control Plane roles.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 12, Key = "permissions.manage", Description = "Assign and remove direct Control Plane user permissions.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 13, Key = "commerce.pages.read", Description = "Read Commerce storefront pages through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 14, Key = "commerce.pages.write", Description = "Create, update, publish, and archive Commerce storefront pages through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 15, Key = "commerce.settings.read", Description = "Read Commerce store configuration through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 16, Key = "commerce.settings.write", Description = "Update Commerce store configuration through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 17, Key = "commerce.features.read", Description = "Read Commerce feature state through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 18, Key = "commerce.features.write", Description = "Update Commerce feature state through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 19, Key = "commerce.providers.read", Description = "Read Commerce provider configuration through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 20, Key = "commerce.providers.write", Description = "Update Commerce provider configuration through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 21, Key = "commerce.navigation.read", Description = "Read Commerce storefront navigation through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 22, Key = "commerce.navigation.write", Description = "Update Commerce storefront navigation through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 23, Key = "commerce.security_privacy.read", Description = "Read Commerce security and privacy settings through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 24, Key = "commerce.security_privacy.write", Description = "Update Commerce security and privacy settings through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 25, Key = "commerce.captcha_settings.edit", Description = "Edit Commerce captcha settings through Control Plane.", CreatedAt = SeedCreatedAt },
                new ControlPlanePermission { Id = 26, Key = "commerce.consent_settings.edit", Description = "Edit Commerce consent settings through Control Plane.", CreatedAt = SeedCreatedAt });

            modelBuilder.Entity<ControlPlaneRolePermission>().HasData(
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 1, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 2, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 3, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 4, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 5, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 6, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 7, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 8, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 9, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 10, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 11, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 12, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 13, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 14, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 15, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 16, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 17, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 18, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 19, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 20, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 21, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 22, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 23, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 24, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 25, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 1, PermissionId = 26, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 1, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 2, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 3, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 4, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 5, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 6, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 7, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 13, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 14, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 15, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 16, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 17, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 18, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 19, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 20, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 21, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 22, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 23, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 24, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 25, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 2, PermissionId = 26, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 1, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 4, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 6, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 7, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 8, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 13, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 15, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 17, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 19, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 21, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 23, CreatedAt = SeedCreatedAt });
        }
    }
}
