using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed class VisualProjectService
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver rootResolver;
    private readonly IVisualSchemaValidator schemaValidator;

    public VisualProjectService(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        rootResolver = new ApprovedArtifactRootResolver(this.repoRoot);
        schemaValidator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<VisualProject> InitializeAsync(
        string referenceUrl,
        string name,
        string outputRoot,
        bool force,
        CancellationToken cancellationToken)
    {
        var url = ReferenceUrl.Create(referenceUrl);
        var projectId = VisualProjectId.Create(name);
        var root = rootResolver.ResolveRoot(outputRoot);
        var projectRoot = rootResolver.ResolveRoot(Path.Combine(root, projectId.Value));

        if (Directory.Exists(projectRoot) && !force)
        {
            throw new InvalidOperationException($"[SRE-INIT-001] Visual project already exists. Problem: '{projectRoot}' already contains project state. Cause: init is non-destructive by default. Fix: choose a new name or pass --force to overwrite deterministic project metadata.");
        }

        if (Directory.Exists(projectRoot) && force)
        {
            DeleteProjectRootForForce(root, projectRoot);
        }

        Directory.CreateDirectory(projectRoot);
        var store = CreateStore(projectRoot);
        var now = DateTimeOffset.UtcNow;

        var project = new VisualProject(
            "1.0",
            "visual-project",
            $"project-{projectId.Value}",
            now,
            projectId.Value,
            name.Trim(),
            url.Value,
            projectRoot,
            VisualProjectStatus.Created,
            UpdatedUtc: now);

        var configuration = new VisualProjectConfiguration(
            "1.0",
            "configuration",
            $"configuration-{projectId.Value}",
            now,
            projectId.Value,
            name.Trim(),
            url.Value,
            root,
            new CapturePolicy(),
            new OriginalityPolicy(),
            ViewportDefinition.Defaults);

        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", project, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("configuration.json"), "configuration", configuration, cancellationToken);
        return project;
    }

    public async Task<VisualProjectInspection> InspectAsync(string projectPath, CancellationToken cancellationToken)
    {
        var projectRoot = rootResolver.ResolveRoot(projectPath);
        var projectFile = Path.Combine(projectRoot, "project.json");
        if (!File.Exists(projectFile))
        {
            throw new InvalidOperationException($"[SRE-INSPECT-001] Visual project was not found. Problem: '{projectPath}' has no project.json. Cause: inspect expects an initialized reverse-engineering project. Fix: run init first or pass the correct --project path.");
        }

        var store = CreateStore(projectRoot);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var latestRun = FindLatestRun(projectRoot);
        var validationSummary = File.Exists(Path.Combine(projectRoot, "reports", "evidence-validation.md"))
            ? "Evidence validation report exists."
            : "No validation report yet.";

        return new VisualProjectInspection(project, latestRun, validationSummary);
    }

    private FileSystemVisualArtifactStore CreateStore(string projectRoot) =>
        new(projectRoot, rootResolver, schemaValidator);

    private static void DeleteProjectRootForForce(string approvedOutputRoot, string projectRoot)
    {
        var fullOutputRoot = Path.GetFullPath(approvedOutputRoot);
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (fullProjectRoot.Equals(fullOutputRoot, StringComparison.OrdinalIgnoreCase) ||
            !ApprovedArtifactRootResolver.IsUnderRoot(fullProjectRoot, fullOutputRoot) ||
            fullProjectRoot.Contains(Path.Combine("storefront-builder", "generated"), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"[SRE-FORCE-001] Unsafe force cleanup refused. Problem: '{projectRoot}' is not a single reverse-engineering project root. Cause: --force may only delete one project under the approved reverse-engineering output root. Fix: pass a project name under artifacts/storefront-reverse-engineering/projects or obj/storefront-reverse-engineering/projects.");
        }

        Directory.Delete(fullProjectRoot, recursive: true);
    }

    private static string? FindLatestRun(string projectRoot)
    {
        var runsRoot = Path.Combine(projectRoot, "runs");
        if (!Directory.Exists(runsRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(runsRoot, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault();
    }
}

public sealed record VisualProjectInspection(
    VisualProject Project,
    string? LatestRunId,
    string ValidationSummary);
