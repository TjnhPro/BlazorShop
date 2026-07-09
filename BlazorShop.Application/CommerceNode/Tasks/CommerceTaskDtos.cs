namespace BlazorShop.Application.CommerceNode.Tasks
{
    public sealed record CommerceTaskListQuery(
        string? Status = null,
        string? TaskType = null,
        DateTimeOffset? CreatedFrom = null,
        DateTimeOffset? CreatedTo = null,
        int Skip = 0,
        int Take = 100);

    public sealed record EnqueueCommerceTaskRequest(
        string TaskType,
        string? IdempotencyKey = null,
        string PayloadSchemaVersion = "v1",
        string? PayloadJson = null,
        string? LockKey = null,
        int MaxAttempts = 3,
        string? CreatedBy = null,
        string? CorrelationId = null);

    public sealed record CancelCommerceTaskRequest(string? Reason = null);

    public sealed record RetryCommerceTaskRequest(string? Reason = null);

    public sealed record CommerceTaskListResponse(
        IReadOnlyList<CommerceTaskSummary> Items,
        int TotalCount,
        int Skip,
        int Take);

    public sealed record CommerceTaskSummary(
        Guid PublicId,
        string TaskType,
        string Status,
        string? IdempotencyKey,
        string? LockKey,
        string PayloadSchemaVersion,
        string? ErrorCode,
        string? ErrorMessage,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset? NextAttemptAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? CreatedBy,
        string? CorrelationId,
        DateTimeOffset? CancelRequestedAt,
        string? WorkerId,
        DateTimeOffset? LastHeartbeatAt);

    public sealed record CommerceTaskDetail(
        Guid PublicId,
        string TaskType,
        string Status,
        string? IdempotencyKey,
        string? LockKey,
        string PayloadSchemaVersion,
        string PayloadJson,
        string? ResultJson,
        string? ErrorCode,
        string? ErrorMessage,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset? NextAttemptAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? CreatedBy,
        string? CorrelationId,
        DateTimeOffset? CancelRequestedAt,
        string? CancelReason,
        string? WorkerId,
        DateTimeOffset? LastHeartbeatAt,
        IReadOnlyList<CommerceTaskStepDto> Steps);

    public sealed record CommerceTaskStepDto(
        Guid Id,
        string StepKey,
        string Status,
        int AttemptNumber,
        string? ResultJson,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt);

    public sealed record CommerceTaskOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        CommerceTaskOperationFailure? Failure = null,
        bool AlreadyExists = false);

    public enum CommerceTaskOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
