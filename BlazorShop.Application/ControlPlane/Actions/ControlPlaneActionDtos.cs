namespace BlazorShop.Application.ControlPlane.Actions
{
    public sealed record ControlPlaneActionListQuery(
        string? Status = null,
        string? ActionType = null,
        Guid? NodePublicId = null,
        Guid? StorePublicId = null,
        long? BeforeId = null,
        int Limit = 100);

    public sealed record EnqueueControlActionRequest(
        Guid NodePublicId,
        string ActionType,
        string? IdempotencyKey = null,
        Guid? StorePublicId = null,
        string? PayloadJson = null,
        string? CorrelationId = null);

    public sealed record RecordControlActionAttemptRequest(
        string Status,
        int? HttpStatusCode = null,
        int DurationMs = 0,
        string? ResponseJson = null,
        string? ErrorCode = null,
        string? ErrorMessage = null);

    public sealed record ControlPlaneActionListResponse(
        IReadOnlyList<ControlPlaneActionSummary> Items,
        long? NextBeforeId);

    public sealed record ControlPlaneActionSummary(
        long Id,
        Guid PublicId,
        string ActionType,
        string Status,
        string IdempotencyKey,
        string? CorrelationId,
        Guid NodePublicId,
        string NodeKey,
        string NodeName,
        Guid? StorePublicId,
        string? StoreKey,
        string? StoreName,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        int AttemptCount);

    public sealed record ControlPlaneActionDetail(
        long Id,
        Guid PublicId,
        string ActionType,
        string Status,
        string IdempotencyKey,
        string? CorrelationId,
        string? PayloadJson,
        string? ResultJson,
        string? ErrorCode,
        string? ErrorMessage,
        string? SuggestedFix,
        Guid NodePublicId,
        string NodeKey,
        string NodeName,
        Guid? StorePublicId,
        string? StoreKey,
        string? StoreName,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        IReadOnlyList<ControlPlaneActionAttemptDto> Attempts);

    public sealed record ControlPlaneActionAttemptDto(
        long Id,
        int AttemptNumber,
        string Status,
        int? HttpStatusCode,
        int DurationMs,
        string? ResponseJson,
        string? ErrorCode,
        string? ErrorMessage,
        string? SuggestedFix,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);

    public sealed record ControlPlaneActionOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneActionOperationFailure? Failure = null,
        bool AlreadyExists = false);

    public enum ControlPlaneActionOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
