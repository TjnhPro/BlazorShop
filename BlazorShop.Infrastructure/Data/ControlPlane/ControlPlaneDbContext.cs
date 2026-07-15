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

            ConfigureIdentity(modelBuilder);
            ConfigureRefreshTokens(modelBuilder);
            ConfigureAdminUsers(modelBuilder);
            ConfigureRolesAndPermissions(modelBuilder);
            ConfigureNodes(modelBuilder);
            ConfigureStores(modelBuilder);
            ConfigureActions(modelBuilder);
            ConfigureAudit(modelBuilder);
            SeedAuthorization(modelBuilder);
        }

        private static void ConfigureIdentity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IdentityUserLogin<string>>()
                .Property(login => login.LoginProvider)
                .HasMaxLength(128);

            modelBuilder.Entity<IdentityUserLogin<string>>()
                .Property(login => login.ProviderKey)
                .HasMaxLength(128);

            modelBuilder.Entity<IdentityUserToken<string>>()
                .Property(token => token.LoginProvider)
                .HasMaxLength(128);

            modelBuilder.Entity<IdentityUserToken<string>>()
                .Property(token => token.Name)
                .HasMaxLength(128);

            modelBuilder.Entity<AppUser>()
                .Property(user => user.CreatedOn)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<AppUser>()
                .Property(user => user.RequirePasswordChange)
                .HasDefaultValue(false);

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "93f5cdac-43de-4895-8426-2048c228e76d",
                    ConcurrencyStamp = "02d86d56-8e63-4d2e-92f8-81b154ba0532",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "b7af6842-02fa-4af4-8f61-ae04a49644a2",
                    ConcurrencyStamp = "75e8afa8-8df5-4431-a220-ac56b1fd0cda",
                    Name = "User",
                    NormalizedName = "USER"
                });
        }

        private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(token => token.Id);
                entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
                entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
                entity.Property(token => token.CreatedByIp).HasMaxLength(64);
                entity.Property(token => token.RevokedByIp).HasMaxLength(64);
                entity.Property(token => token.UserAgent).HasMaxLength(512);
                entity.Property(token => token.CreatedAtUtc).HasColumnType("timestamp with time zone");
                entity.Property(token => token.ExpiresAtUtc).HasColumnType("timestamp with time zone");
                entity.Property(token => token.RevokedAtUtc).HasColumnType("timestamp with time zone");
                entity.HasIndex(token => token.TokenHash).IsUnique();
                entity.HasIndex(token => new { token.UserId, token.RevokedAtUtc });
                entity.HasIndex(token => token.ExpiresAtUtc);
                entity.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureAdminUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ControlPlaneAdminUser>(entity =>
            {
                entity.ToTable(
                    "control_plane_admin_user",
                    table => table.HasCheckConstraint(
                        "ck_control_plane_admin_user_status",
                        "status in ('active', 'disabled', 'invited')"));

                entity.HasKey(user => user.Id);
                entity.Property(user => user.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(user => user.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(user => user.IdentityUserId).HasColumnName("identity_user_id").HasColumnType("text").IsRequired();
                entity.Property(user => user.Email).HasColumnName("email").HasColumnType("text").IsRequired();
                entity.Property(user => user.DisplayName).HasColumnName("display_name").HasColumnType("text").IsRequired();
                entity.Property(user => user.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(user => user.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamp with time zone");
                entity.Property(user => user.StatusChangedAt).HasColumnName("status_changed_at").HasColumnType("timestamp with time zone");
                entity.Property(user => user.StatusChangedByAdminUserId).HasColumnName("status_changed_by_admin_user_id");
                entity.Property(user => user.StatusReason).HasColumnName("status_reason").HasColumnType("text");
                entity.Property(user => user.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(user => user.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(user => user.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(user => user.PublicId)
                    .IsUnique()
                    .HasDatabaseName("control_plane_admin_user_public_id_uq");

                entity.HasIndex(user => user.IdentityUserId).IsUnique();
                entity.HasIndex(user => user.Status)
                    .HasDatabaseName("ix_control_plane_admin_user_status")
                    .HasFilter("deleted_at is null");
                entity.HasIndex(user => user.StatusChangedByAdminUserId)
                    .HasDatabaseName("ix_control_plane_admin_user_status_changed_by");
                entity.HasIndex(user => user.Email)
                    .IsUnique()
                    .HasDatabaseName("control_plane_admin_user_active_email_uq")
                    .HasFilter("deleted_at is null");

                entity.HasOne(user => user.StatusChangedByAdminUser)
                    .WithMany()
                    .HasForeignKey(user => user.StatusChangedByAdminUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureRolesAndPermissions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ControlPlaneRole>(entity =>
            {
                entity.ToTable(
                    "control_plane_role",
                    table => table.HasCheckConstraint("ck_control_plane_role_key_lower", "key = lower(key)"));

                entity.HasKey(role => role.Id);
                entity.Property(role => role.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(role => role.Key).HasColumnName("key").HasColumnType("text").IsRequired();
                entity.Property(role => role.Name).HasColumnName("name").HasColumnType("text").IsRequired();
                entity.Property(role => role.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(role => role.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
                entity.Property(role => role.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(role => role.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(role => role.Key).IsUnique();
            });

            modelBuilder.Entity<ControlPlanePermission>(entity =>
            {
                entity.ToTable(
                    "control_plane_permission",
                    table => table.HasCheckConstraint("ck_control_plane_permission_key_lower", "key = lower(key)"));

                entity.HasKey(permission => permission.Id);
                entity.Property(permission => permission.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(permission => permission.Key).HasColumnName("key").HasColumnType("text").IsRequired();
                entity.Property(permission => permission.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(permission => permission.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(permission => permission.Key).IsUnique();
            });

            modelBuilder.Entity<ControlPlaneAdminUserRole>(entity =>
            {
                entity.ToTable("control_plane_admin_user_role");
                entity.HasKey(userRole => new { userRole.AdminUserId, userRole.RoleId });
                entity.Property(userRole => userRole.AdminUserId).HasColumnName("admin_user_id");
                entity.Property(userRole => userRole.RoleId).HasColumnName("role_id");
                entity.Property(userRole => userRole.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(userRole => userRole.RoleId);
                entity.HasOne(userRole => userRole.AdminUser)
                    .WithMany(user => user.Roles)
                    .HasForeignKey(userRole => userRole.AdminUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(userRole => userRole.Role)
                    .WithMany(role => role.Users)
                    .HasForeignKey(userRole => userRole.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ControlPlaneRolePermission>(entity =>
            {
                entity.ToTable("control_plane_role_permission");
                entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
                entity.Property(rolePermission => rolePermission.RoleId).HasColumnName("role_id");
                entity.Property(rolePermission => rolePermission.PermissionId).HasColumnName("permission_id");
                entity.Property(rolePermission => rolePermission.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(rolePermission => rolePermission.PermissionId);
                entity.HasOne(rolePermission => rolePermission.Role)
                    .WithMany(role => role.Permissions)
                    .HasForeignKey(rolePermission => rolePermission.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(rolePermission => rolePermission.Permission)
                    .WithMany(permission => permission.Roles)
                    .HasForeignKey(rolePermission => rolePermission.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ControlPlaneAdminUserPermission>(entity =>
            {
                entity.ToTable("control_plane_admin_user_permission");
                entity.HasKey(userPermission => new { userPermission.AdminUserId, userPermission.PermissionId });
                entity.Property(userPermission => userPermission.AdminUserId).HasColumnName("admin_user_id");
                entity.Property(userPermission => userPermission.PermissionId).HasColumnName("permission_id");
                entity.Property(userPermission => userPermission.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(userPermission => userPermission.CreatedByAdminUserId).HasColumnName("created_by_admin_user_id");
                entity.HasIndex(userPermission => userPermission.PermissionId)
                    .HasDatabaseName("ix_control_plane_admin_user_permission_permission_id");
                entity.HasIndex(userPermission => userPermission.CreatedByAdminUserId)
                    .HasDatabaseName("ix_control_plane_admin_user_permission_created_by");
                entity.HasOne(userPermission => userPermission.AdminUser)
                    .WithMany(user => user.DirectPermissions)
                    .HasForeignKey(userPermission => userPermission.AdminUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(userPermission => userPermission.Permission)
                    .WithMany(permission => permission.DirectUsers)
                    .HasForeignKey(userPermission => userPermission.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(userPermission => userPermission.CreatedByAdminUser)
                    .WithMany(user => user.CreatedDirectPermissionGrants)
                    .HasForeignKey(userPermission => userPermission.CreatedByAdminUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureNodes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommerceNode>(entity =>
            {
                entity.ToTable(
                    "commerce_node",
                    table => table.HasCheckConstraint(
                        "ck_commerce_node_status",
                        "status in ('unknown', 'healthy', 'warning', 'down', 'disabled')"));

                entity.HasKey(node => node.Id);
                entity.Property(node => node.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(node => node.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(node => node.NodeKey).HasColumnName("node_key").HasColumnType("text").IsRequired();
                entity.Property(node => node.NodeSecret).HasColumnName("node_secret").HasColumnType("text");
                entity.Property(node => node.NodeSecretUpdatedAt).HasColumnName("node_secret_updated_at").HasColumnType("timestamp with time zone");
                entity.Property(node => node.Name).HasColumnName("name").HasColumnType("text").IsRequired();
                entity.Property(node => node.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(node => node.Description).HasColumnName("description").HasColumnType("text");
                entity.Property(node => node.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamp with time zone");
                entity.Property(node => node.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(node => node.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(node => node.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");
                entity.HasIndex(node => node.PublicId).IsUnique();
                entity.HasIndex(node => node.NodeKey)
                    .IsUnique()
                    .HasDatabaseName("commerce_node_active_node_key_uq")
                    .HasFilter("disabled_at is null");
                entity.HasIndex(node => node.Status);
            });

            modelBuilder.Entity<CommerceNodeEndpoint>(entity =>
            {
                entity.ToTable(
                    "commerce_node_endpoint",
                    table => table.HasCheckConstraint(
                        "ck_commerce_node_endpoint_kind",
                        "kind in ('control_api', 'storefront', 'internal_api')"));

                entity.HasKey(endpoint => endpoint.Id);
                entity.Property(endpoint => endpoint.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(endpoint => endpoint.NodeId).HasColumnName("node_id");
                entity.Property(endpoint => endpoint.Kind).HasColumnName("kind").HasColumnType("text").IsRequired();
                entity.Property(endpoint => endpoint.Url).HasColumnName("url").HasColumnType("text").IsRequired();
                entity.Property(endpoint => endpoint.IsPrimary).HasColumnName("is_primary");
                entity.Property(endpoint => endpoint.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(endpoint => endpoint.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(endpoint => endpoint.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");
                entity.HasIndex(endpoint => endpoint.NodeId);
                entity.HasIndex(endpoint => new { endpoint.NodeId, endpoint.Kind })
                    .HasDatabaseName("commerce_node_endpoint_active_kind_idx")
                    .HasFilter("disabled_at is null");
                entity.HasOne(endpoint => endpoint.Node)
                    .WithMany(node => node.Endpoints)
                    .HasForeignKey(endpoint => endpoint.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CommerceNodeCredential>(entity =>
            {
                entity.ToTable(
                    "commerce_node_credential",
                    table => table.HasCheckConstraint(
                        "ck_commerce_node_credential_status",
                        "status in ('active', 'revoked', 'rotated')"));

                entity.HasKey(credential => credential.Id);
                entity.Property(credential => credential.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(credential => credential.NodeId).HasColumnName("node_id");
                entity.Property(credential => credential.KeyId).HasColumnName("key_id").HasColumnType("text").IsRequired();
                entity.Property(credential => credential.SecretHash).HasColumnName("secret_hash").HasColumnType("text").IsRequired();
                entity.Property(credential => credential.HashAlgorithm).HasColumnName("hash_algorithm").HasColumnType("text").IsRequired();
                entity.Property(credential => credential.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(credential => credential.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(credential => credential.RevealedAt).HasColumnName("revealed_at").HasColumnType("timestamp with time zone");
                entity.Property(credential => credential.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
                entity.Property(credential => credential.CreatedByAdminUserId).HasColumnName("created_by_admin_user_id");
                entity.Property(credential => credential.RevokedByAdminUserId).HasColumnName("revoked_by_admin_user_id");
                entity.HasIndex(credential => credential.NodeId);
                entity.HasIndex(credential => credential.CreatedByAdminUserId);
                entity.HasIndex(credential => credential.RevokedByAdminUserId);
                entity.HasIndex(credential => credential.KeyId).IsUnique();
                entity.HasIndex(credential => new { credential.NodeId, credential.Status })
                    .HasDatabaseName("commerce_node_credential_active_node_idx")
                    .HasFilter("revoked_at is null");
                entity.HasOne(credential => credential.Node)
                    .WithMany(node => node.Credentials)
                    .HasForeignKey(credential => credential.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(credential => credential.CreatedByAdminUser)
                    .WithMany()
                    .HasForeignKey(credential => credential.CreatedByAdminUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(credential => credential.RevokedByAdminUser)
                    .WithMany()
                    .HasForeignKey(credential => credential.RevokedByAdminUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<NodeHealthSnapshot>(entity =>
            {
                entity.ToTable(
                    "node_health_snapshot",
                    table => table.HasCheckConstraint(
                        "ck_node_health_snapshot_status",
                        "status in ('healthy', 'warning', 'down', 'timeout', 'malformed', 'unknown')"));

                entity.HasKey(snapshot => snapshot.Id);
                entity.Property(snapshot => snapshot.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(snapshot => snapshot.NodeId).HasColumnName("node_id");
                entity.Property(snapshot => snapshot.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(snapshot => snapshot.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(snapshot => snapshot.HttpStatusCode).HasColumnName("http_status_code");
                entity.Property(snapshot => snapshot.DurationMs).HasColumnName("duration_ms");
                entity.Property(snapshot => snapshot.DependencyStatusJson).HasColumnName("dependency_status_json").HasColumnType("jsonb");
                entity.Property(snapshot => snapshot.ErrorCode).HasColumnName("error_code").HasColumnType("text");
                entity.Property(snapshot => snapshot.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
                entity.Property(snapshot => snapshot.CheckedAt).HasColumnName("checked_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(snapshot => snapshot.PublicId).IsUnique();
                entity.HasIndex(snapshot => snapshot.NodeId);
                entity.HasIndex(snapshot => new { snapshot.NodeId, snapshot.CheckedAt });
                entity.HasIndex(snapshot => new { snapshot.Status, snapshot.CheckedAt });
                entity.HasOne(snapshot => snapshot.Node)
                    .WithMany(node => node.HealthSnapshots)
                    .HasForeignKey(snapshot => snapshot.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NodeCapabilitySnapshot>(entity =>
            {
                entity.ToTable("node_capability_snapshot");
                entity.HasKey(snapshot => snapshot.Id);
                entity.Property(snapshot => snapshot.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(snapshot => snapshot.NodeId).HasColumnName("node_id");
                entity.Property(snapshot => snapshot.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(snapshot => snapshot.SchemaVersion).HasColumnName("schema_version").HasColumnType("text").IsRequired();
                entity.Property(snapshot => snapshot.Checksum).HasColumnName("checksum").HasColumnType("text").IsRequired();
                entity.Property(snapshot => snapshot.CapabilitiesJson).HasColumnName("capabilities_json").HasColumnType("jsonb").IsRequired();
                entity.Property(snapshot => snapshot.IsCurrent).HasColumnName("is_current");
                entity.Property(snapshot => snapshot.CapturedAt).HasColumnName("captured_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(snapshot => snapshot.PublicId).IsUnique();
                entity.HasIndex(snapshot => snapshot.NodeId);
                entity.HasIndex(snapshot => new { snapshot.NodeId, snapshot.IsCurrent })
                    .HasDatabaseName("node_capability_snapshot_current_idx")
                    .HasFilter("is_current");
                entity.HasIndex(snapshot => new { snapshot.NodeId, snapshot.Checksum });
                entity.HasOne(snapshot => snapshot.Node)
                    .WithMany(node => node.CapabilitySnapshots)
                    .HasForeignKey(snapshot => snapshot.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureStores(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoreRegistry>(entity =>
            {
                entity.ToTable(
                    "store_registry",
                    table => table.HasCheckConstraint(
                        "ck_store_registry_status",
                        "status in ('active', 'provisioning', 'disabled', 'archived')"));

                entity.HasKey(store => store.Id);
                entity.Property(store => store.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(store => store.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(store => store.NodeId).HasColumnName("node_id");
                entity.Property(store => store.StoreKey).HasColumnName("store_key").HasColumnType("text").IsRequired();
                entity.Property(store => store.Name).HasColumnName("name").HasColumnType("text").IsRequired();
                entity.Property(store => store.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(store => store.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
                entity.Property(store => store.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(store => store.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(store => store.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");
                entity.HasIndex(store => store.PublicId).IsUnique();
                entity.HasIndex(store => store.NodeId);
                entity.HasIndex(store => new { store.NodeId, store.StoreKey })
                    .IsUnique()
                    .HasDatabaseName("store_registry_active_node_store_key_uq")
                    .HasFilter("archived_at is null");
                entity.HasOne(store => store.Node)
                    .WithMany(node => node.Stores)
                    .HasForeignKey(store => store.NodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StoreDomainRegistry>(entity =>
            {
                entity.ToTable(
                    "store_domain_registry",
                    table => table.HasCheckConstraint(
                        "ck_store_domain_registry_status",
                        "status in ('pending', 'verified', 'disabled')"));

                entity.HasKey(domain => domain.Id);
                entity.Property(domain => domain.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(domain => domain.StoreId).HasColumnName("store_id");
                entity.Property(domain => domain.Domain).HasColumnName("domain").HasColumnType("text").IsRequired();
                entity.Property(domain => domain.NormalizedDomain).HasColumnName("normalized_domain").HasColumnType("text").IsRequired();
                entity.Property(domain => domain.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(domain => domain.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(domain => domain.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(domain => domain.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
                entity.Property(domain => domain.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");
                entity.HasIndex(domain => domain.StoreId);
                entity.HasIndex(domain => domain.NormalizedDomain)
                    .IsUnique()
                    .HasDatabaseName("store_domain_registry_active_domain_uq")
                    .HasFilter("disabled_at is null");
                entity.HasOne(domain => domain.Store)
                    .WithMany(store => store.Domains)
                    .HasForeignKey(domain => domain.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureActions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ControlAction>(entity =>
            {
                entity.ToTable(
                    "control_action",
                    table => table.HasCheckConstraint(
                        "ck_control_action_status",
                        "status in ('queued', 'running', 'failed', 'succeeded', 'cancelled')"));

                entity.HasKey(action => action.Id);
                entity.Property(action => action.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(action => action.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(action => action.NodeId).HasColumnName("node_id");
                entity.Property(action => action.StoreId).HasColumnName("store_id");
                entity.Property(action => action.ActionType).HasColumnName("action_type").HasColumnType("text").IsRequired();
                entity.Property(action => action.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(action => action.IdempotencyKey).HasColumnName("idempotency_key").HasColumnType("text").IsRequired();
                entity.Property(action => action.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
                entity.Property(action => action.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
                entity.Property(action => action.ErrorCode).HasColumnName("error_code").HasColumnType("text");
                entity.Property(action => action.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
                entity.Property(action => action.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(action => action.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(action => action.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
                entity.Property(action => action.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
                entity.HasIndex(action => action.PublicId).IsUnique();
                entity.HasIndex(action => action.NodeId);
                entity.HasIndex(action => action.StoreId);
                entity.HasIndex(action => new { action.NodeId, action.IdempotencyKey }).IsUnique();
                entity.HasIndex(action => new { action.Status, action.CreatedAt });
                entity.HasOne(action => action.Node)
                    .WithMany(node => node.Actions)
                    .HasForeignKey(action => action.NodeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(action => action.Store)
                    .WithMany()
                    .HasForeignKey(action => action.StoreId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ControlActionAttempt>(entity =>
            {
                entity.ToTable(
                    "control_action_attempt",
                    table => table.HasCheckConstraint(
                        "ck_control_action_attempt_status",
                        "status in ('running', 'failed', 'succeeded', 'cancelled')"));

                entity.HasKey(attempt => attempt.Id);
                entity.Property(attempt => attempt.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(attempt => attempt.ActionId).HasColumnName("action_id");
                entity.Property(attempt => attempt.AttemptNumber).HasColumnName("attempt_number");
                entity.Property(attempt => attempt.Status).HasColumnName("status").HasColumnType("text").IsRequired();
                entity.Property(attempt => attempt.HttpStatusCode).HasColumnName("http_status_code");
                entity.Property(attempt => attempt.DurationMs).HasColumnName("duration_ms");
                entity.Property(attempt => attempt.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb");
                entity.Property(attempt => attempt.ErrorCode).HasColumnName("error_code").HasColumnType("text");
                entity.Property(attempt => attempt.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
                entity.Property(attempt => attempt.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(attempt => attempt.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
                entity.HasIndex(attempt => attempt.ActionId);
                entity.HasIndex(attempt => new { attempt.ActionId, attempt.AttemptNumber }).IsUnique();
                entity.HasOne(attempt => attempt.Action)
                    .WithMany(action => action.Attempts)
                    .HasForeignKey(attempt => attempt.ActionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureAudit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ControlAuditLog>(entity =>
            {
                entity.ToTable(
                    "control_audit_log",
                    table => table.HasCheckConstraint(
                        "ck_control_audit_log_result",
                        "result in ('success', 'failure', 'denied')"));

                entity.HasKey(log => log.Id);
                entity.Property(log => log.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(log => log.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(log => log.ActorAdminUserId).HasColumnName("actor_admin_user_id");
                entity.Property(log => log.ActorIdentityUserId).HasColumnName("actor_identity_user_id").HasColumnType("text");
                entity.Property(log => log.ActorEmail).HasColumnName("actor_email").HasColumnType("text");
                entity.Property(log => log.Action).HasColumnName("action").HasColumnType("text").IsRequired();
                entity.Property(log => log.EntityType).HasColumnName("entity_type").HasColumnType("text").IsRequired();
                entity.Property(log => log.EntityPublicId).HasColumnName("entity_public_id").HasColumnType("text");
                entity.Property(log => log.NodeId).HasColumnName("node_id");
                entity.Property(log => log.StoreId).HasColumnName("store_id");
                entity.Property(log => log.ControlActionId).HasColumnName("control_action_id");
                entity.Property(log => log.Result).HasColumnName("result").HasColumnType("text").IsRequired();
                entity.Property(log => log.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
                entity.Property(log => log.IpAddress).HasColumnName("ip_address").HasColumnType("text");
                entity.Property(log => log.UserAgent).HasColumnName("user_agent").HasColumnType("text");
                entity.Property(log => log.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(log => log.PublicId).IsUnique();
                entity.HasIndex(log => log.ActorAdminUserId);
                entity.HasIndex(log => log.NodeId);
                entity.HasIndex(log => log.StoreId);
                entity.HasIndex(log => log.ControlActionId);
                entity.HasIndex(log => new { log.Action, log.CreatedAt });
                entity.HasIndex(log => new { log.ActorEmail, log.CreatedAt });
                entity.HasIndex(log => log.CreatedAt);
                entity.HasOne(log => log.ActorAdminUser)
                    .WithMany()
                    .HasForeignKey(log => log.ActorAdminUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(log => log.Node)
                    .WithMany()
                    .HasForeignKey(log => log.NodeId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(log => log.Store)
                    .WithMany()
                    .HasForeignKey(log => log.StoreId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(log => log.ControlAction)
                    .WithMany()
                    .HasForeignKey(log => log.ControlActionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
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
                new ControlPlanePermission { Id = 22, Key = "commerce.navigation.write", Description = "Update Commerce storefront navigation through Control Plane.", CreatedAt = SeedCreatedAt });

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
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 1, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 4, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 6, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 7, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 8, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 13, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 15, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 17, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 19, CreatedAt = SeedCreatedAt },
                new ControlPlaneRolePermission { RoleId = 3, PermissionId = 21, CreatedAt = SeedCreatedAt });
        }
    }
}
