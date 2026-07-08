namespace BlazorShop.Application.ControlPlane.Stores
{
    public sealed record ControlPlaneStoreListQuery(
        string? Search = null,
        string? Status = null,
        Guid? NodePublicId = null);

    public sealed record CreateControlPlaneStoreRequest(
        string StoreKey,
        string Name,
        Guid NodePublicId,
        string? MetadataJson = null);

    public sealed record UpdateControlPlaneStoreRequest(
        string Name,
        Guid NodePublicId,
        string? MetadataJson = null);

    public sealed record CreateControlPlaneStoreDomainRequest(string Domain);

    public sealed record ControlPlaneStoreListResponse(IReadOnlyList<ControlPlaneStoreSummary> Items);

    public sealed record ControlPlaneStoreSummary(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        Guid NodePublicId,
        string NodeKey,
        string NodeName,
        string NodeStatus,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ArchivedAt,
        int DomainCount);

    public sealed record ControlPlaneStoreDetail(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? MetadataJson,
        Guid NodePublicId,
        string NodeKey,
        string NodeName,
        string NodeStatus,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ArchivedAt,
        IReadOnlyList<ControlPlaneStoreDomainDto> Domains);

    public sealed record ControlPlaneStoreDomainDto(
        long Id,
        string Domain,
        string NormalizedDomain,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? VerifiedAt,
        DateTimeOffset? DisabledAt);

    public sealed record ControlPlaneStoreOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneStoreOperationFailure? Failure = null);

    public enum ControlPlaneStoreOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
