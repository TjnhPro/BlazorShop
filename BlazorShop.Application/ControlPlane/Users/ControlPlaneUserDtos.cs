namespace BlazorShop.Application.ControlPlane.Users
{
    public sealed record ControlPlaneUserListQuery(
        string? Search = null,
        string? Status = null,
        string? RoleKey = null,
        string? PermissionKey = null,
        string? Cursor = null,
        int Limit = 25);

    public sealed record ControlPlaneUserListResponse(
        IReadOnlyList<ControlPlaneUserSummary> Items,
        string? NextCursor);

    public sealed record CreateControlPlaneUserRequest(
        string Email,
        string DisplayName,
        string? IdentityRole,
        IReadOnlyList<string>? ControlPlaneRoleKeys,
        IReadOnlyList<string>? DirectPermissionKeys,
        string? TemporaryPassword);

    public sealed record CreateControlPlaneUserResponse(
        ControlPlaneUserDetail User,
        string? TemporaryPassword);

    public sealed record UpdateControlPlaneUserRequest(
        string DisplayName);

    public sealed record ChangeControlPlaneUserStatusRequest(
        string? Reason);

    public sealed record AssignControlPlaneRoleRequest(
        string RoleKey);

    public sealed record AssignControlPlanePermissionRequest(
        string PermissionKey);

    public sealed record ControlPlaneUserActor(
        string? IdentityUserId,
        string? Email);

    public sealed record ControlPlaneUserSummary(
        Guid PublicId,
        string Email,
        string DisplayName,
        string Status,
        IReadOnlyList<string> IdentityRoles,
        IReadOnlyList<ControlPlaneUserRoleAssignment> ControlPlaneRoles,
        IReadOnlyList<string> DirectPermissionKeys,
        IReadOnlyList<string> EffectivePermissionKeys,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record ControlPlaneUserDetail(
        Guid PublicId,
        string IdentityUserId,
        string Email,
        string DisplayName,
        string Status,
        string? StatusReason,
        DateTimeOffset? StatusChangedAt,
        IReadOnlyList<string> IdentityRoles,
        IReadOnlyList<ControlPlaneUserRoleAssignment> ControlPlaneRoles,
        IReadOnlyList<ControlPlaneDirectPermissionGrant> DirectPermissions,
        IReadOnlyList<ControlPlaneEffectivePermission> EffectivePermissions,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record ControlPlaneUserRoleAssignment(
        string Key,
        string Name,
        string? Description);

    public sealed record ControlPlaneDirectPermissionGrant(
        string Key,
        string? Description,
        DateTimeOffset CreatedAt);

    public sealed record ControlPlaneEffectivePermission(
        string Key,
        string? Description,
        IReadOnlyList<string> Sources);

    public sealed record ControlPlaneRoleCatalogResponse(
        IReadOnlyList<ControlPlaneRoleCatalogItem> Items);

    public sealed record ControlPlaneRoleCatalogItem(
        string Key,
        string Name,
        string? Description,
        bool IsSystem,
        IReadOnlyList<string> PermissionKeys);

    public sealed record ControlPlanePermissionCatalogResponse(
        IReadOnlyList<ControlPlanePermissionCatalogItem> Items);

    public sealed record ControlPlanePermissionCatalogItem(
        string Key,
        string? Description);

    public sealed record ControlPlaneUserOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneUserOperationFailure Failure = ControlPlaneUserOperationFailure.None);

    public enum ControlPlaneUserOperationFailure
    {
        None,
        Validation,
        Conflict,
        NotFound
    }
}
