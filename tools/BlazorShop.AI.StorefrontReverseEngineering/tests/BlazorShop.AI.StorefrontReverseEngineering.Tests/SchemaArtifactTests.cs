using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class SchemaArtifactTests
{
    [Fact]
    public void SchemaRegistry_RegistersRequiredArtifactKinds()
    {
        var registry = new VisualSchemaRegistry();
        var kinds = registry.Schemas.Select(schema => schema.ArtifactKind).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("visual-project", kinds);
        Assert.Contains("configuration", kinds);
        Assert.Contains("reference-site-profile", kinds);
        Assert.Contains("reconnaissance", kinds);
        Assert.Contains("capture-plan", kinds);
        Assert.Contains("capture-viewport-manifest", kinds);
        Assert.Contains("page-capture-manifest", kinds);
        Assert.Contains("capture-manifest", kinds);
        Assert.Contains("screenshot-evidence", kinds);
        Assert.Contains("dom-evidence", kinds);
        Assert.Contains("computed-style-evidence", kinds);
        Assert.Contains("element-box-evidence", kinds);
        Assert.Contains("element-evidence-index", kinds);
        Assert.Contains("asset-inventory", kinds);
        Assert.Contains("interaction-evidence", kinds);
        Assert.Contains("page-topology-draft", kinds);
        Assert.Contains("page-specification-draft", kinds);
        Assert.Contains("component-specification-draft", kinds);
        Assert.Contains("visual-blueprint-draft", kinds);
        Assert.Contains("originality-audit", kinds);
        Assert.Contains("readiness-report", kinds);
        Assert.Contains("workflow-run", kinds);
        Assert.Contains("skill-definition", kinds);
    }

    [Fact]
    public void SchemaRegistry_LoadsSchemaFilesForFirstClassArtifacts()
    {
        var repoRoot = GetRepoRoot();
        var schemaRoot = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas");
        var registry = new VisualSchemaRegistry();

        foreach (var schema in registry.Schemas.Where(schema => schema.ArtifactKind != "capture-manifest"))
        {
            Assert.True(File.Exists(Path.Combine(schemaRoot, schema.ArtifactKind + ".schema.json")), schema.ArtifactKind);
        }
    }

    [Fact]
    public void SchemaValidator_RejectsInvalidSchema()
    {
        var validator = new VisualSchemaValidator(new VisualSchemaRegistry());
        var artifact = JsonNode.Parse("""{"schemaVersion":"1.0","artifactKind":"visual-project","createdUtc":"2026-01-01T00:00:00Z"}""")!;

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate("visual-project", artifact));
        Assert.Contains("SRE-SCHEMA-006", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaValidator_RejectsMissingDomainField()
    {
        var validator = new VisualSchemaValidator(new VisualSchemaRegistry());
        var artifact = JsonNode.Parse("""{"schemaVersion":"1.0","artifactKind":"visual-project","artifactId":"project-demo","createdUtc":"2026-01-01T00:00:00Z","projectId":"demo"}""")!;

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate("visual-project", artifact));
        Assert.Contains("SRE-SCHEMA-011", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaValidator_RejectsInvalidNestedArrayShape()
    {
        var validator = new VisualSchemaValidator(new VisualSchemaRegistry());
        var artifact = JsonNode.Parse("""{"schemaVersion":"1.0","artifactKind":"capture-plan","artifactId":"capture-plan-demo","createdUtc":"2026-01-01T00:00:00Z","projectId":"demo","pages":{},"viewports":[]}""")!;

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate("capture-plan", artifact));
        Assert.Contains("SRE-SCHEMA-012", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaValidator_RejectsInvalidEnumAndStaleVersion()
    {
        var validator = new VisualSchemaValidator(new VisualSchemaRegistry());
        var invalidEnum = JsonNode.Parse("""{"schemaVersion":"1.0","artifactKind":"workflow-run","artifactId":"run-demo","createdUtc":"2026-01-01T00:00:00Z","projectId":"demo","runId":"run-1","status":"Done","steps":[],"updatedUtc":"2026-01-01T00:00:01Z"}""")!;
        var stale = JsonNode.Parse("""{"schemaVersion":"0.9","artifactKind":"workflow-run","artifactId":"run-demo","createdUtc":"2026-01-01T00:00:00Z","projectId":"demo","runId":"run-1","status":"Succeeded","steps":[],"updatedUtc":"2026-01-01T00:00:01Z"}""")!;

        var enumException = Assert.Throws<InvalidOperationException>(() => validator.Validate("workflow-run", invalidEnum));
        var staleException = Assert.Throws<InvalidOperationException>(() => validator.Validate("workflow-run", stale));
        Assert.Contains("SRE-SCHEMA-013", enumException.Message, StringComparison.Ordinal);
        Assert.Contains("SRE-SCHEMA-008", staleException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArtifactPath_RejectsTraversal()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ArtifactPath.Create("../project.json"));
        Assert.Contains("SRE-PATH-001", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RootResolver_ApprovesManualAndAutomationRoots()
    {
        var repoRoot = GetRepoRoot();
        var resolver = new ApprovedArtifactRootResolver(repoRoot);

        Assert.EndsWith(Path.Combine("artifacts", "storefront-reverse-engineering", "projects"), resolver.ResolveRoot("artifacts/storefront-reverse-engineering/projects"), StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("obj", "storefront-reverse-engineering", "projects"), resolver.ResolveRoot("obj/storefront-reverse-engineering/projects"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArtifactStore_WritesReadsAndValidatesTypedJson()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "artifact-test-" + Guid.NewGuid().ToString("N"));
        var store = new FileSystemVisualArtifactStore(
            outputRoot,
            new ApprovedArtifactRootResolver(repoRoot),
            new VisualSchemaValidator(new VisualSchemaRegistry()));

        var project = new VisualProject(
            "1.0",
            "visual-project",
            "project-demo",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            "demo",
            "Demo",
            "https://example.test/",
            outputRoot,
            Domain.VisualProjectStatus.Created);

        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", project, CancellationToken.None);
        var roundTrip = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", CancellationToken.None);

        Assert.Equal(project.ProjectId, roundTrip.ProjectId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "configuration", CancellationToken.None));
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
}
