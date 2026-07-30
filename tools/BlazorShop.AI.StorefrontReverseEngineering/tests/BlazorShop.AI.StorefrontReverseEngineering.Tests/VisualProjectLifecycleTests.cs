using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class VisualProjectLifecycleTests
{
    [Fact]
    public async Task Init_CreatesProjectAndConfiguration()
    {
        var service = new VisualProjectService(GetRepoRoot());
        var outputRoot = CreateOutputRoot();

        var project = await service.InitializeAsync("https://example.test", "Demo Store", outputRoot, force: false, CancellationToken.None);

        Assert.Equal("demo-store", project.ProjectId);
        Assert.Equal(VisualProjectStatus.Created, project.Status);
        Assert.True(File.Exists(Path.Combine(project.ArtifactRoot, "project.json")));
        Assert.True(File.Exists(Path.Combine(project.ArtifactRoot, "configuration.json")));
    }

    [Fact]
    public async Task Init_RejectsDuplicateProjectWithoutForce()
    {
        var service = new VisualProjectService(GetRepoRoot());
        var outputRoot = CreateOutputRoot();
        await service.InitializeAsync("https://example.test", "Duplicate", outputRoot, force: false, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitializeAsync("https://example.test", "Duplicate", outputRoot, force: false, CancellationToken.None));

        Assert.Contains("SRE-INIT-001", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Init_RejectsInvalidUrl()
    {
        var service = new VisualProjectService(GetRepoRoot());
        var outputRoot = CreateOutputRoot();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.InitializeAsync("ftp://example.test", "Invalid", outputRoot, force: false, CancellationToken.None));
    }

    [Fact]
    public async Task Inspect_RejectsMissingProject()
    {
        var service = new VisualProjectService(GetRepoRoot());
        var missingProject = Path.Combine(CreateOutputRoot(), "missing");
        Directory.CreateDirectory(missingProject);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InspectAsync(missingProject, CancellationToken.None));

        Assert.Contains("SRE-INSPECT-001", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cli_InitAndInspect_PrintProjectState()
    {
        var outputRoot = CreateOutputRoot();
        using var initOut = new StringWriter();
        using var initErr = new StringWriter();

        var initCode = await CliHost.RunAsync(
            ["init", "--url", "https://example.test", "--name", "Cli Demo", "--output-root", outputRoot],
            initOut,
            initErr,
            CancellationToken.None);

        Assert.Equal(0, initCode);
        Assert.Contains("Visual project initialized: cli-demo", initOut.ToString(), StringComparison.Ordinal);

        using var inspectOut = new StringWriter();
        using var inspectErr = new StringWriter();
        var inspectCode = await CliHost.RunAsync(
            ["inspect", "--project", Path.Combine(outputRoot, "cli-demo")],
            inspectOut,
            inspectErr,
            CancellationToken.None);

        Assert.Equal(0, inspectCode);
        Assert.Contains("Status: Created", inspectOut.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void StatusTransition_RejectsInvalidTransitionsWithoutRecoveryMode()
    {
        var project = new Contracts.VisualProject(
            "1.0",
            "visual-project",
            "project-demo",
            DateTimeOffset.UtcNow,
            "demo",
            "Demo",
            "https://example.test/",
            "obj/storefront-reverse-engineering/projects/demo",
            VisualProjectStatus.Created);

        Assert.Throws<InvalidOperationException>(() =>
            VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.Captured));
        Assert.Equal(VisualProjectStatus.Captured, VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.Captured, recoveryMode: true).Status);
    }

    private static string CreateOutputRoot() =>
        Path.Combine("obj", "storefront-reverse-engineering", "projects", "lifecycle-test-" + Guid.NewGuid().ToString("N"));

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
