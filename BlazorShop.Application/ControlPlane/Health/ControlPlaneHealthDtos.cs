namespace BlazorShop.Application.ControlPlane.Health
{
    public sealed record ControlPlaneHealthListResponse(IReadOnlyList<ControlPlaneHealthNodeSummary> Items);

    public sealed record ControlPlaneHealthNodeSummary(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        DateTimeOffset? LastSeenAt,
        ControlPlaneHealthSnapshotDto? LatestHealth,
        ControlPlaneCapabilitySnapshotDto? CurrentCapability);

    public sealed record ControlPlaneHealthDetail(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        DateTimeOffset? LastSeenAt,
        IReadOnlyList<ControlPlaneHealthSnapshotDto> RecentHealth,
        ControlPlaneCapabilitySnapshotDto? CurrentCapability);

    public sealed record ControlPlaneHealthSnapshotDto(
        Guid PublicId,
        string Status,
        int? HttpStatusCode,
        int DurationMs,
        string? DependencyStatusJson,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset CheckedAt);

    public sealed record ControlPlaneCapabilitySnapshotDto(
        Guid PublicId,
        string SchemaVersion,
        string Checksum,
        string CapabilitiesJson,
        bool IsCurrent,
        DateTimeOffset CapturedAt);

    public sealed record ControlPlaneProbeResult(
        ControlPlaneHealthSnapshotDto Health,
        ControlPlaneCapabilitySnapshotDto? Capability,
        bool CapabilityChanged);

    public sealed record ControlPlaneHealthOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneHealthOperationFailure? Failure = null);

    public enum ControlPlaneHealthOperationFailure
    {
        Validation,
        NotFound
    }
}
