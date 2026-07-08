namespace BlazorShop.Domain.Entities.ControlPlane
{
    public sealed class ControlPlaneAdminUser
    {
        public long Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string IdentityUserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public DateTimeOffset? LastLoginAt { get; set; }

        public DateTimeOffset? StatusChangedAt { get; set; }

        public long? StatusChangedByAdminUserId { get; set; }

        public ControlPlaneAdminUser? StatusChangedByAdminUser { get; set; }

        public string? StatusReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<ControlPlaneAdminUserRole> Roles { get; set; } = new List<ControlPlaneAdminUserRole>();

        public ICollection<ControlPlaneAdminUserPermission> DirectPermissions { get; set; } = new List<ControlPlaneAdminUserPermission>();

        public ICollection<ControlPlaneAdminUserPermission> CreatedDirectPermissionGrants { get; set; } = new List<ControlPlaneAdminUserPermission>();
    }

    public sealed class ControlPlaneRole
    {
        public long Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsSystem { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public ICollection<ControlPlaneAdminUserRole> Users { get; set; } = new List<ControlPlaneAdminUserRole>();

        public ICollection<ControlPlaneRolePermission> Permissions { get; set; } = new List<ControlPlaneRolePermission>();
    }

    public sealed class ControlPlanePermission
    {
        public long Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<ControlPlaneRolePermission> Roles { get; set; } = new List<ControlPlaneRolePermission>();

        public ICollection<ControlPlaneAdminUserPermission> DirectUsers { get; set; } = new List<ControlPlaneAdminUserPermission>();
    }

    public sealed class ControlPlaneAdminUserRole
    {
        public long AdminUserId { get; set; }

        public ControlPlaneAdminUser? AdminUser { get; set; }

        public long RoleId { get; set; }

        public ControlPlaneRole? Role { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class ControlPlaneRolePermission
    {
        public long RoleId { get; set; }

        public ControlPlaneRole? Role { get; set; }

        public long PermissionId { get; set; }

        public ControlPlanePermission? Permission { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class ControlPlaneAdminUserPermission
    {
        public long AdminUserId { get; set; }

        public ControlPlaneAdminUser? AdminUser { get; set; }

        public long PermissionId { get; set; }

        public ControlPlanePermission? Permission { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public long? CreatedByAdminUserId { get; set; }

        public ControlPlaneAdminUser? CreatedByAdminUser { get; set; }
    }

    public sealed class CommerceNode
    {
        public long Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string NodeKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = "unknown";

        public string? Description { get; set; }

        public DateTimeOffset? LastSeenAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? DisabledAt { get; set; }

        public ICollection<CommerceNodeEndpoint> Endpoints { get; set; } = new List<CommerceNodeEndpoint>();

        public ICollection<CommerceNodeCredential> Credentials { get; set; } = new List<CommerceNodeCredential>();

        public ICollection<NodeHealthSnapshot> HealthSnapshots { get; set; } = new List<NodeHealthSnapshot>();

        public ICollection<NodeCapabilitySnapshot> CapabilitySnapshots { get; set; } = new List<NodeCapabilitySnapshot>();

        public ICollection<StoreRegistry> Stores { get; set; } = new List<StoreRegistry>();

        public ICollection<ControlAction> Actions { get; set; } = new List<ControlAction>();
    }

    public sealed class CommerceNodeEndpoint
    {
        public long Id { get; set; }

        public long NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public string Kind { get; set; } = "control_api";

        public string Url { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? DisabledAt { get; set; }
    }

    public sealed class CommerceNodeCredential
    {
        public long Id { get; set; }

        public long NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public string KeyId { get; set; } = string.Empty;

        public string SecretHash { get; set; } = string.Empty;

        public string HashAlgorithm { get; set; } = "sha256";

        public string Status { get; set; } = "active";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? RevealedAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public long? CreatedByAdminUserId { get; set; }

        public ControlPlaneAdminUser? CreatedByAdminUser { get; set; }

        public long? RevokedByAdminUserId { get; set; }

        public ControlPlaneAdminUser? RevokedByAdminUser { get; set; }
    }

    public sealed class NodeHealthSnapshot
    {
        public long Id { get; set; }

        public long NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string Status { get; set; } = "unknown";

        public int? HttpStatusCode { get; set; }

        public int DurationMs { get; set; }

        public string? DependencyStatusJson { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTimeOffset CheckedAt { get; set; }
    }

    public sealed class NodeCapabilitySnapshot
    {
        public long Id { get; set; }

        public long NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string SchemaVersion { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;

        public string CapabilitiesJson { get; set; } = "{}";

        public bool IsCurrent { get; set; }

        public DateTimeOffset CapturedAt { get; set; }
    }

    public sealed class StoreRegistry
    {
        public long Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public long NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public string StoreKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public string? MetadataJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? ArchivedAt { get; set; }

        public ICollection<StoreDomainRegistry> Domains { get; set; } = new List<StoreDomainRegistry>();
    }

    public sealed class StoreDomainRegistry
    {
        public long Id { get; set; }

        public long StoreId { get; set; }

        public StoreRegistry? Store { get; set; }

        public string Domain { get; set; } = string.Empty;

        public string NormalizedDomain { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? VerifiedAt { get; set; }

        public DateTimeOffset? DisabledAt { get; set; }
    }

    public sealed class ControlAction
    {
        public long Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public long NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public long? StoreId { get; set; }

        public StoreRegistry? Store { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public string Status { get; set; } = "queued";

        public string IdempotencyKey { get; set; } = string.Empty;

        public string? PayloadJson { get; set; }

        public string? ResultJson { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        public ICollection<ControlActionAttempt> Attempts { get; set; } = new List<ControlActionAttempt>();
    }

    public sealed class ControlActionAttempt
    {
        public long Id { get; set; }

        public long ActionId { get; set; }

        public ControlAction? Action { get; set; }

        public int AttemptNumber { get; set; }

        public string Status { get; set; } = "running";

        public int? HttpStatusCode { get; set; }

        public int DurationMs { get; set; }

        public string? ResponseJson { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }

    public sealed class ControlAuditLog
    {
        public long Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public long? ActorAdminUserId { get; set; }

        public ControlPlaneAdminUser? ActorAdminUser { get; set; }

        public string? ActorIdentityUserId { get; set; }

        public string? ActorEmail { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public string? EntityPublicId { get; set; }

        public long? NodeId { get; set; }

        public CommerceNode? Node { get; set; }

        public long? StoreId { get; set; }

        public StoreRegistry? Store { get; set; }

        public long? ControlActionId { get; set; }

        public ControlAction? ControlAction { get; set; }

        public string Result { get; set; } = "success";

        public string? MetadataJson { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
