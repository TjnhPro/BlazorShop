namespace BlazorShop.Application.ControlPlane.Credentials
{
    public sealed record ControlPlaneCredentialListQuery(
        int PageNumber = 1,
        int PageSize = 25);

    public sealed record ControlPlaneCredentialSummary(
        string KeyId,
        string Status,
        string HashAlgorithm,
        DateTimeOffset CreatedAt,
        DateTimeOffset? RevealedAt,
        DateTimeOffset? RevokedAt);

    public sealed record ControlPlaneCredentialListResponse(
        IReadOnlyList<ControlPlaneCredentialSummary> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

    public sealed record CreateControlPlaneCredentialRequest(string? Note = null);

    public sealed record ControlPlaneCredentialSecretResult(
        ControlPlaneCredentialSummary Credential,
        string RawSecret);

}
