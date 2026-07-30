namespace BlazorShop.AI.StorefrontReverseEngineering.Workflows;

public interface IWorkflowStep<in TContext>
{
    string Name { get; }

    Task<WorkflowStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken);
}

public sealed record WorkflowStepResult(
    bool Succeeded,
    bool Retryable = false,
    bool Skipped = false,
    IReadOnlyList<WorkflowMessage>? Warnings = null,
    IReadOnlyList<WorkflowMessage>? Errors = null)
{
    public static WorkflowStepResult Success(params WorkflowMessage[] warnings) => new(true, Warnings: warnings);

    public static WorkflowStepResult Failure(string code, string message, bool retryable = false) =>
        new(false, retryable, Errors: [new WorkflowMessage(code, message)]);

    public static WorkflowStepResult Skip(string code, string message) =>
        new(true, Skipped: true, Warnings: [new WorkflowMessage(code, message)]);
}

public sealed record WorkflowMessage(string Code, string Message);

public enum WorkflowRunStatus
{
    Running,
    Succeeded,
    Failed,
    Canceled
}

public enum WorkflowStepStatus
{
    Pending,
    Running,
    Succeeded,
    Skipped,
    Failed,
    Canceled
}

public sealed record WorkflowRun(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string RunId,
    WorkflowRunStatus Status,
    IReadOnlyList<WorkflowStepRecord> Steps,
    DateTimeOffset UpdatedUtc);

public sealed record WorkflowStepRecord(
    string Name,
    WorkflowStepStatus Status,
    int RetryCount,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    IReadOnlyList<WorkflowMessage> Warnings,
    IReadOnlyList<WorkflowMessage> Errors);
