using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;

namespace BlazorShop.AI.StorefrontReverseEngineering.Cli;

public static class CliHost
{
    private static readonly string[] KnownCommands =
    [
        "init",
        "discover",
        "capture",
        "analyze",
        "inspect",
        "validate",
        "run"
    ];

    public static Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            WriteHelp(output);
            return Task.FromResult(0);
        }

        var command = args[0].Trim().ToLowerInvariant();
        if (!KnownCommands.Contains(command, StringComparer.Ordinal))
        {
            error.WriteLine($"[SRE-CLI-001] Unknown command '{args[0]}'. Problem: command is not supported. Cause: Phase 3A only exposes known workflow commands. Fix: run with --help and choose a listed command.");
            return Task.FromResult(2);
        }

        return RunCommandAsync(command, args[1..], output, error, cancellationToken);
    }

    public static void WriteHelp(TextWriter output)
    {
        output.WriteLine("BlazorShop.AI.StorefrontReverseEngineering");
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering -- <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");
        foreach (var command in KnownCommands)
        {
            output.WriteLine($"  {command}");
        }
    }

    private static async Task<int> RunCommandAsync(
        string command,
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = CommandOptions.Parse(args);
            var service = new VisualProjectService(FindRepositoryRoot());
            switch (command)
            {
                case "init":
                    var project = await service.InitializeAsync(
                        options.GetRequired("url", "SRE-INIT-002"),
                        options.GetRequired("name", "SRE-INIT-003"),
                        options.GetRequired("output-root", "SRE-INIT-004"),
                        options.HasFlag("force"),
                        cancellationToken);
                    output.WriteLine($"Visual project initialized: {project.ProjectId}");
                    output.WriteLine($"Status: {project.Status}");
                    output.WriteLine($"Artifact root: {project.ArtifactRoot}");
                    return 0;
                case "inspect":
                    var inspection = await service.InspectAsync(
                        options.GetRequired("project", "SRE-INSPECT-002"),
                        cancellationToken);
                    output.WriteLine($"Project: {inspection.Project.ProjectId}");
                    output.WriteLine($"Name: {inspection.Project.Name}");
                    output.WriteLine($"Status: {inspection.Project.Status}");
                    output.WriteLine($"Source URL: {inspection.Project.ReferenceUrl}");
                    output.WriteLine($"Artifact root: {inspection.Project.ArtifactRoot}");
                    output.WriteLine($"Latest run: {inspection.LatestRunId ?? "(none)"}");
                    output.WriteLine($"Validation: {inspection.ValidationSummary}");
                    return 0;
                case "discover":
                    var projectPath = options.GetRequired("project", "SRE-DISCOVER-001");
                    var projectInspection = await service.InspectAsync(projectPath, cancellationToken);
                    var discoveryService = new VisualDiscoveryService(
                        FindRepositoryRoot(),
                        ReferenceBrowserFactory.Create(FindRepositoryRoot(), projectInspection.Project.ReferenceUrl));
                    var result = await discoveryService.DiscoverAsync(projectPath, cancellationToken);
                    output.WriteLine($"Discovery completed: {result.SiteProfile.ProjectId}");
                    output.WriteLine($"Title: {result.SiteProfile.Title ?? "(unknown)"}");
                    output.WriteLine($"Blockers: {result.Reconnaissance.Blockers.Count}");
                    output.WriteLine($"Capture pages: {result.CapturePlan.Pages.Count}");
                    return 0;
                case "capture":
                    var captured = await new VisualProjectWorkflowService(FindRepositoryRoot())
                        .CaptureAsync(options.GetRequired("project", "SRE-CAPTURE-001"), cancellationToken);
                    output.WriteLine($"Capture completed: {captured} viewport(s)");
                    return 0;
                case "analyze":
                    var blueprint = await new VisualProjectWorkflowService(FindRepositoryRoot())
                        .AnalyzeAsync(options.GetRequired("project", "SRE-ANALYZE-001"), options.HasFlag("no-ai"), cancellationToken);
                    output.WriteLine($"Analysis completed: {blueprint.ArtifactId}");
                    return 0;
                case "validate":
                    var report = await new VisualProjectWorkflowService(FindRepositoryRoot())
                        .ValidateAsync(options.GetRequired("project", "SRE-VALIDATE-001"), cancellationToken);
                    output.WriteLine($"Validation passed: {report.Passed}");
                    output.WriteLine($"Findings: {report.Findings.Count}");
                    return report.Passed ? 0 : 3;
                case "run":
                    var summary = await new VisualProjectWorkflowService(FindRepositoryRoot()).RunAsync(
                        options.GetRequired("url", "SRE-RUN-001"),
                        options.GetRequired("name", "SRE-RUN-002"),
                        options.GetRequired("output-root", "SRE-RUN-003"),
                        options.HasFlag("force"),
                        options.HasFlag("resume"),
                        options.HasFlag("no-ai"),
                        cancellationToken);
                    output.WriteLine($"Run completed: {summary.ProjectId}");
                    output.WriteLine($"Artifact root: {summary.ArtifactRoot}");
                    output.WriteLine($"Captured viewports: {summary.CapturedViewports}");
                    output.WriteLine($"Blueprint: {summary.BlueprintArtifactId}");
                    output.WriteLine($"Readiness passed: {summary.ReadinessPassed}");
                    return summary.ReadinessPassed ? 0 : 3;
                default:
                    output.WriteLine($"StorefrontReverseEngineering command '{command}' is available. Implementation is added by later Phase 3A workflow phases.");
                    return 0;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Environment.CurrentDirectory;
    }
}
