namespace BlazorShop.Application.ControlPlane.Nodes
{
    public sealed record ControlPlaneNodeListQuery(
        string? Search = null,
        string? Status = null,
        string? Cursor = null,
        int Limit = 25);

    public sealed record CreateControlPlaneNodeRequest(
        string NodeKey,
        string NodeSecret,
        string Name,
        string? Description,
        string ControlApiUrl);

    public sealed record UpdateControlPlaneNodeRequest(
        string Name,
        string? Description,
        string ControlApiUrl,
        string? NodeSecret = null);

    public sealed record ControlPlaneNodeListResponse(
        IReadOnlyList<ControlPlaneNodeSummary> Items,
        string? NextCursor);

    public sealed record ControlPlaneNodeSummary(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        string? Description,
        string? ControlApiUrl,
        bool HasNodeSecret,
        DateTimeOffset? NodeSecretUpdatedAt,
        DateTimeOffset? LastSeenAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? DisabledAt);

    public sealed record ControlPlaneNodeDetail(
        Guid PublicId,
        string NodeKey,
        string Name,
        string Status,
        string? Description,
        string? ControlApiUrl,
        bool HasNodeSecret,
        DateTimeOffset? NodeSecretUpdatedAt,
        DateTimeOffset? LastSeenAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? DisabledAt,
        IReadOnlyList<ControlPlaneNodeEndpointDto> Endpoints);

    public sealed record ControlPlaneNodeEndpointDto(
        long Id,
        string Kind,
        string Url,
        bool IsPrimary,
        DateTimeOffset? DisabledAt);

    public sealed record ControlPlaneNodeOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneNodeOperationFailure Failure = ControlPlaneNodeOperationFailure.None);

    public enum ControlPlaneNodeOperationFailure
    {
        None,
        Validation,
        Conflict,
        NotFound
    }
}
