namespace BlazorShop.Application.ControlPlane.Credentials
{
    public sealed record ControlPlaneCredentialSummary(
        string KeyId,
        string Status,
        string HashAlgorithm,
        DateTimeOffset CreatedAt,
        DateTimeOffset? RevealedAt,
        DateTimeOffset? RevokedAt);

    public sealed record ControlPlaneCredentialListResponse(IReadOnlyList<ControlPlaneCredentialSummary> Items);

    public sealed record CreateControlPlaneCredentialRequest(string? Note = null);

    public sealed record ControlPlaneCredentialSecretResult(
        ControlPlaneCredentialSummary Credential,
        string RawSecret);

    public sealed record ControlPlaneCredentialOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneCredentialOperationFailure? Failure = null);

    public enum ControlPlaneCredentialOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
