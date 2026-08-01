namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public static class AgentHandoffContract
{
    public const string HandoffRoot = "analysis/agent-handoff";
    public const string PackageVersion = "phase3e-portable-handoff-v1";

    public static IReadOnlyList<RequiredHandoffArtifact> RequiredArtifacts { get; } =
    [
        new("analysis/agent-handoff/manifest.json", "agent-handoff-manifest", "agent-handoff-manifest", "application/json", "always", HashRequired: false, IsDirectory: false),
        Text("analysis/agent-handoff/task.md"),
        Json("analysis/agent-handoff/allowed-files.json", "allowed-files", "allowed-files"),
        Json("analysis/agent-handoff/protected-files.json", "protected-files", "protected-files"),
        Json("analysis/agent-handoff/page-compositions.json", "agent-handoff-page-compositions", "agent-handoff-page-compositions"),
        Json("analysis/agent-handoff/visual-style.json", "agent-handoff-visual-style", "agent-handoff-visual-style"),
        Json("analysis/agent-handoff/design-tokens.json", "agent-handoff-design-tokens", "agent-handoff-design-tokens"),
        Json("analysis/agent-handoff/storefront-pattern.json", "storefront-pattern", "storefront-pattern"),
        Json("analysis/agent-handoff/visual-blueprint.json", "agent-handoff-visual-blueprint", "agent-handoff-visual-blueprint"),
        Json("analysis/agent-handoff/presentation-catalog.json", "agent-handoff-presentation-catalog", "agent-handoff-presentation-catalog"),
        Json("analysis/agent-handoff/presentation-mappings.json", "reviewed-presentation-mappings", "reviewed-presentation-mappings"),
        Json("analysis/agent-handoff/component-candidates.json", "reviewed-component-candidates", "reviewed-component-candidates"),
        Json("analysis/agent-handoff/component-instances.json", "component-instances", "component-instances"),
        Json("analysis/agent-handoff/responsive-behavior.json", "agent-handoff-responsive-behavior", "agent-handoff-responsive-behavior"),
        Json("analysis/agent-handoff/interaction-models.json", "agent-handoff-interaction-models", "agent-handoff-interaction-models"),
        Json("analysis/agent-handoff/originality-restrictions.json", "reviewed-originality-restrictions", "reviewed-originality-restrictions"),
        Json("analysis/agent-handoff/confidence.json", "confidence-report", "confidence-report"),
        Json("analysis/agent-handoff/review-resolution.json", "agent-handoff-review-resolution", "agent-handoff-review-resolution"),
        Json("analysis/agent-handoff/unresolved-regions.json", "unresolved-regions", "unresolved-regions"),
        Json("analysis/agent-handoff/generation-readiness.json", "generation-readiness", "generation-readiness"),
        Json("analysis/agent-handoff/handoff-readiness.json", "agent-handoff-readiness", "agent-handoff-readiness"),
        Json("analysis/agent-handoff/evidence-manifest.json", "agent-handoff-evidence-manifest", "agent-handoff-evidence-manifest"),
        Directory("analysis/agent-handoff/screenshots/"),
        Directory("analysis/agent-handoff/section-screenshots/")
    ];

    public static IReadOnlyList<RequiredHandoffSchema> RequiredSchemaKinds { get; } =
        LoadRequiredSchemas()
            .DistinctBy(schema => schema.SchemaKind, StringComparer.Ordinal)
            .OrderBy(schema => schema.SchemaKind, StringComparer.Ordinal)
            .ToArray();

    private static RequiredHandoffArtifact Json(string relativePath, string artifactKind, string schemaName) =>
        new(relativePath, artifactKind, schemaName, "application/json", "always", HashRequired: true, IsDirectory: false);

    private static RequiredHandoffArtifact Text(string relativePath) =>
        new(relativePath, "markdown", "markdown", "text/markdown", "always", HashRequired: true, IsDirectory: false);

    private static RequiredHandoffArtifact Directory(string relativePath) =>
        new(relativePath, "directory", "directory", "inode/directory", "when evidence exists", HashRequired: false, IsDirectory: true);

    private static IEnumerable<RequiredHandoffSchema> LoadRequiredSchemas()
    {
        var schemaRoot = FindSchemaRoot();
        foreach (var artifact in RequiredArtifacts.Where(artifact => artifact.ContentType == "application/json"))
        {
            var schemaFileName = $"{artifact.SchemaName}.schema.json";
            var schemaPath = schemaRoot is null ? null : Path.Combine(schemaRoot, schemaFileName);
            yield return new RequiredHandoffSchema(
                artifact.SchemaName,
                artifact.ArtifactKind,
                "1.0",
                schemaFileName,
                schemaPath is not null && File.Exists(schemaPath) ? PortableHandoffPackageHasher.ComputeFileHash(schemaPath) : string.Empty,
                Required: true);
        }
    }

    private static string? FindSchemaRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Schemas"),
            Path.Combine(Environment.CurrentDirectory, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"),
            Path.Combine(Environment.CurrentDirectory, "Schemas")
        };

        foreach (var candidate in candidates)
        {
            if (System.IO.Directory.Exists(candidate) && System.IO.Directory.EnumerateFiles(candidate, "*.schema.json").Any())
            {
                return candidate;
            }
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas");
            if (System.IO.Directory.Exists(candidate) && System.IO.Directory.EnumerateFiles(candidate, "*.schema.json").Any())
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

public sealed record RequiredHandoffArtifact(
    string RelativePath,
    string ArtifactKind,
    string SchemaName,
    string ContentType,
    string RequiredCondition,
    bool HashRequired,
    bool IsDirectory);

public sealed record RequiredHandoffSchema(
    string SchemaKind,
    string ArtifactKind,
    string SchemaVersion,
    string SchemaFileName,
    string Sha256,
    bool Required);
