namespace BlazorShop.Application.ControlPlane.Audit
{
    public sealed record ControlPlaneAuditEntry(
        string Action,
        string EntityType,
        string Result,
        string? ActorIdentityUserId = null,
        string? ActorEmail = null,
        long? ActorAdminUserId = null,
        long? NodeId = null,
        long? StoreId = null,
        long? ControlActionId = null,
        string? EntityPublicId = null,
        string? MetadataJson = null,
        string? IpAddress = null,
        string? UserAgent = null);
}
