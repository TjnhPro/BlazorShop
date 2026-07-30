using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class WorkflowRunnerTests
{
    [Fact]
    public async Task Workflow_SuccessfulSequentialRun_PersistsSucceededState()
    {
        var calls = new List<string>();
        var runner = CreateRunner();

        var run = await runner.RunAsync(
            "demo",
            Guid.NewGuid().ToString("N"),
            calls,
            [new RecordingStep("one"), new RecordingStep("two")],
            cancellationToken: CancellationToken.None);

        Assert.Equal(WorkflowRunStatus.Succeeded, run.Status);
        Assert.Equal(["one", "two"], calls);
        Assert.All(run.Steps, step => Assert.Equal(WorkflowStepStatus.Succeeded, step.Status));
    }

    [Fact]
    public async Task Workflow_FailedStep_StopsDownstreamSteps()
    {
        var calls = new List<string>();
        var runner = CreateRunner();

        var run = await runner.RunAsync(
            "demo",
            Guid.NewGuid().ToString("N"),
            calls,
            [new RecordingStep("one"), new FailingStep("two"), new RecordingStep("three")],
            cancellationToken: CancellationToken.None);

        Assert.Equal(WorkflowRunStatus.Failed, run.Status);
        Assert.Equal(["one"], calls);
        Assert.Equal(WorkflowStepStatus.Pending, run.Steps.Single(step => step.Name == "three").Status);
    }

    [Fact]
    public async Task Workflow_RetryableFailure_RetriesAndSucceeds()
    {
        var calls = new List<string>();
        var runner = CreateRunner();
        var step = new FlakyStep("flaky");

        var run = await runner.RunAsync(
            "demo",
            Guid.NewGuid().ToString("N"),
            calls,
            [step],
            cancellationToken: CancellationToken.None);

        Assert.Equal(WorkflowRunStatus.Succeeded, run.Status);
        Assert.Equal(2, step.Attempts);
        Assert.Equal(1, run.Steps.Single().RetryCount);
    }

    [Fact]
    public async Task Workflow_ResumeAfterPartialSuccess_SkipsCompletedSteps()
    {
        var runId = Guid.NewGuid().ToString("N");
        var store = CreateStore();
        var firstCalls = new List<string>();
        var firstRun = await new SequentialWorkflowRunner<List<string>>(store).RunAsync(
            "demo",
            runId,
            firstCalls,
            [new RecordingStep("one"), new FailingStep("two")],
            cancellationToken: CancellationToken.None);
        Assert.Equal(WorkflowRunStatus.Failed, firstRun.Status);

        var secondCalls = new List<string>();
        var secondRun = await new SequentialWorkflowRunner<List<string>>(store).RunAsync(
            "demo",
            runId,
            secondCalls,
            [new RecordingStep("one"), new RecordingStep("two")],
            cancellationToken: CancellationToken.None);

        Assert.Equal(WorkflowRunStatus.Succeeded, secondRun.Status);
        Assert.Equal(["two"], secondCalls);
    }

    [Fact]
    public async Task Workflow_ForceStep_RerunsSelectedStepAndDownstreamSteps()
    {
        var runId = Guid.NewGuid().ToString("N");
        var store = CreateStore();
        var firstCalls = new List<string>();
        await new SequentialWorkflowRunner<List<string>>(store).RunAsync(
            "demo",
            runId,
            firstCalls,
            [new RecordingStep("one"), new RecordingStep("two"), new RecordingStep("three")],
            cancellationToken: CancellationToken.None);

        var secondCalls = new List<string>();
        var secondRun = await new SequentialWorkflowRunner<List<string>>(store).RunAsync(
            "demo",
            runId,
            secondCalls,
            [new RecordingStep("one"), new RecordingStep("two"), new RecordingStep("three")],
            forceStep: "two",
            cancellationToken: CancellationToken.None);

        Assert.Equal(WorkflowRunStatus.Succeeded, secondRun.Status);
        Assert.Equal(["two", "three"], secondCalls);
    }


    [Fact]
    public async Task Workflow_Cancellation_IsNotLoggedAsTimeout()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        var run = await CreateRunner().RunAsync(
            "demo",
            Guid.NewGuid().ToString("N"),
            new List<string>(),
            [new RecordingStep("one")],
            cancellationToken: source.Token);

        Assert.Equal(WorkflowRunStatus.Canceled, run.Status);
        Assert.DoesNotContain("timeout", string.Join(' ', run.Steps.SelectMany(step => step.Errors.Select(error => error.Message))), StringComparison.OrdinalIgnoreCase);
    }

    private static SequentialWorkflowRunner<List<string>> CreateRunner() => new(CreateStore());

    private static FileSystemVisualArtifactStore CreateStore()
    {
        var repoRoot = GetRepoRoot();
        return new FileSystemVisualArtifactStore(
            Path.Combine("obj", "storefront-reverse-engineering", "projects", "workflow-test-" + Guid.NewGuid().ToString("N")),
            new ApprovedArtifactRootResolver(repoRoot),
            new VisualSchemaValidator(new VisualSchemaRegistry()));
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class RecordingStep(string name) : IWorkflowStep<List<string>>
    {
        public string Name => name;

        public Task<WorkflowStepResult> ExecuteAsync(List<string> context, CancellationToken cancellationToken)
        {
            context.Add(Name);
            return Task.FromResult(WorkflowStepResult.Success());
        }
    }

    private sealed class FailingStep(string name) : IWorkflowStep<List<string>>
    {
        public string Name => name;

        public Task<WorkflowStepResult> ExecuteAsync(List<string> context, CancellationToken cancellationToken) =>
            Task.FromResult(WorkflowStepResult.Failure("TEST-FAIL", "step failed"));
    }

    private sealed class FlakyStep(string name) : IWorkflowStep<List<string>>
    {
        public string Name => name;

        public int Attempts { get; private set; }

        public Task<WorkflowStepResult> ExecuteAsync(List<string> context, CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromResult(Attempts == 1
                ? WorkflowStepResult.Failure("TEST-FLAKY", "try again", retryable: true)
                : WorkflowStepResult.Success());
        }
    }
}
