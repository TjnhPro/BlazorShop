namespace BlazorShop.Application.ControlPlane.Nodes
{
    public sealed record ControlPlaneNodeListQuery(
        string? Search = null,
        string? Status = null,
        int PageNumber = 1,
        int PageSize = 25);

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
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

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

}
