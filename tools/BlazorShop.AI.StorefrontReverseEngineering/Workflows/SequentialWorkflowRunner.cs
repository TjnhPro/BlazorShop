using BlazorShop.AI.StorefrontReverseEngineering.Storage;

namespace BlazorShop.AI.StorefrontReverseEngineering.Workflows;

public sealed class SequentialWorkflowRunner<TContext>
{
    private readonly IVisualArtifactStore store;
    private readonly int maximumRetries;

    public SequentialWorkflowRunner(IVisualArtifactStore store, int maximumRetries = 1)
    {
        this.store = store;
        this.maximumRetries = Math.Max(0, maximumRetries);
    }

    public async Task<WorkflowRun> RunAsync(
        string projectId,
        string runId,
        TContext context,
        IReadOnlyList<IWorkflowStep<TContext>> steps,
        string? forceStep = null,
        CancellationToken cancellationToken = default)
    {
        var run = await LoadOrCreateRunAsync(projectId, runId, steps, CancellationToken.None);

        try
        {
            var forceActive = false;
            foreach (var step in steps)
            {
                var existing = run.Steps.First(record => record.Name == step.Name);
                if (!forceActive && string.Equals(forceStep, step.Name, StringComparison.Ordinal))
                {
                    forceActive = true;
                }

                if (existing.Status is WorkflowStepStatus.Succeeded or WorkflowStepStatus.Skipped &&
                    !forceActive)
                {
                    continue;
                }

                run = UpdateStep(run, step.Name, existing with
                {
                    Status = WorkflowStepStatus.Running,
                    StartedUtc = DateTimeOffset.UtcNow,
                    Errors = [],
                    Warnings = []
                }) with { Status = WorkflowRunStatus.Running };
                await PersistAsync(run, cancellationToken);

                WorkflowStepResult result;
                var retryCount = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result = await step.ExecuteAsync(context, cancellationToken);
                    if (result.Succeeded || !result.Retryable || retryCount >= maximumRetries)
                    {
                        break;
                    }

                    retryCount++;
                }

                var completed = new WorkflowStepRecord(
                    step.Name,
                    result.Skipped ? WorkflowStepStatus.Skipped : result.Succeeded ? WorkflowStepStatus.Succeeded : WorkflowStepStatus.Failed,
                    retryCount,
                    existing.StartedUtc ?? DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    result.Warnings ?? [],
                    result.Errors ?? []);

                run = UpdateStep(run, step.Name, completed);
                if (!result.Succeeded)
                {
                    run = run with { Status = WorkflowRunStatus.Failed, UpdatedUtc = DateTimeOffset.UtcNow };
                    await PersistAsync(run, cancellationToken);
                    return run;
                }

                await PersistAsync(run with { UpdatedUtc = DateTimeOffset.UtcNow }, cancellationToken);
            }

            run = run with { Status = WorkflowRunStatus.Succeeded, UpdatedUtc = DateTimeOffset.UtcNow };
            await PersistAsync(run, cancellationToken);
            return run;
        }
        catch (OperationCanceledException)
        {
            var runningStep = run.Steps.FirstOrDefault(step => step.Status == WorkflowStepStatus.Running);
            if (runningStep is not null)
            {
                run = UpdateStep(run, runningStep.Name, runningStep with
                {
                    Status = WorkflowStepStatus.Canceled,
                    CompletedUtc = DateTimeOffset.UtcNow,
                    Errors = [new WorkflowMessage("SRE-WORKFLOW-CANCELED", "Workflow was canceled by the caller and recorded as caller cancellation.")]
                });
            }

            run = run with { Status = WorkflowRunStatus.Canceled, UpdatedUtc = DateTimeOffset.UtcNow };
            await PersistAsync(run, CancellationToken.None);
            return run;
        }
    }

    private async Task<WorkflowRun> LoadOrCreateRunAsync(
        string projectId,
        string runId,
        IReadOnlyList<IWorkflowStep<TContext>> steps,
        CancellationToken cancellationToken)
    {
        var path = ArtifactPath.Create($"runs/{runId}.json");
        try
        {
            return await store.ReadJsonAsync<WorkflowRun>(path, "workflow-run", cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return CreateRun(projectId, runId, steps);
        }
        catch (DirectoryNotFoundException)
        {
            return CreateRun(projectId, runId, steps);
        }
    }

    private static WorkflowRun CreateRun(
        string projectId,
        string runId,
        IReadOnlyList<IWorkflowStep<TContext>> steps)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowRun(
            "1.0",
            "workflow-run",
            $"workflow-run-{runId}",
            now,
            projectId,
            runId,
            WorkflowRunStatus.Running,
            steps.Select(step => new WorkflowStepRecord(step.Name, WorkflowStepStatus.Pending, 0, null, null, [], [])).ToArray(),
            now);
    }

    private async Task PersistAsync(WorkflowRun run, CancellationToken cancellationToken) =>
        await store.WriteJsonAsync(ArtifactPath.Create($"runs/{run.RunId}.json"), "workflow-run", run, cancellationToken);

    private static WorkflowRun UpdateStep(WorkflowRun run, string name, WorkflowStepRecord updated)
    {
        return run with
        {
            Steps = run.Steps.Select(step => step.Name == name ? updated : step).ToArray(),
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }
}
