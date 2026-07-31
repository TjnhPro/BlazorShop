using System.Security.Cryptography;
using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed class AgentHandoffAssembler
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public AgentHandoffAssembler(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(this.repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<AgentHandoffManifest> AssembleAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var readiness = await store.ReadJsonAsync<GenerationReadinessReport>(ArtifactPath.Create("reports/generation-readiness.json"), "generation-readiness", cancellationToken);
        var compositions = await store.ReadJsonAsync<ReviewedPageCompositionsDocument>(ArtifactPath.Create("analysis/resolved/page-compositions.reviewed.json"), "reviewed-page-compositions", cancellationToken);
        var createdUtc = project.CreatedUtc;
        var allowed = BuildAllowedFiles(project.ProjectId, createdUtc, compositions);
        var protectedFiles = BuildProtectedFiles(project.ProjectId, createdUtc);
        var unresolved = BuildUnresolved(project.ProjectId, createdUtc, readiness);
        await new AgentHandoffEvidencePackager()
            .PackageAsync(root, project.ProjectId, createdUtc, compositions, cancellationToken);

        await WriteAsync(root, "allowed-files.json", allowed, cancellationToken);
        await WriteAsync(root, "protected-files.json", protectedFiles, cancellationToken);
        await WriteAsync(root, "unresolved-regions.json", unresolved, cancellationToken);
        await CopyAsync(root, "analysis/resolved/page-compositions.reviewed.json", "page-compositions.json", cancellationToken);
        await CopyAsync(root, "analysis/resolved/semantic-tokens.reviewed.json", "design-tokens.json", cancellationToken);
        await CopyAsync(root, "analysis/resolved/semantic-tokens.reviewed.json", "visual-style.json", cancellationToken);
        await CopyAsync(root, "analysis/storefront-pattern/storefront-pattern.json", "storefront-pattern.json", cancellationToken);
        await CopyAsync(root, "analysis/visual-blueprint.v1.reviewed.json", "visual-blueprint.json", cancellationToken);
        await CopyAsync(root, "reports/generation-readiness.json", "generation-readiness.json", cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(root, AgentHandoffContract.HandoffRoot.Replace('/', Path.DirectorySeparatorChar), "task.md"),
            WriteTask(project, readiness, allowed, protectedFiles, compositions, ReadPageContracts(root)),
            cancellationToken);

        var manifest = new AgentHandoffManifest(
            "1.0",
            "agent-handoff-manifest",
            $"agent-handoff-{project.ProjectId}",
            createdUtc,
            project.ProjectId,
            root,
            "diagnostics-only",
            AgentHandoffContract.HandoffRoot,
            project.LatestRunId,
            TryGetGitSha(),
            "phase3d-agent-handoff-v2",
            readiness.Passed,
            ReadReviewBundleHash(root),
            FileHash(root, "analysis/agent-handoff/storefront-pattern.json"),
            FileHash(root, "presentation-catalog/presentation-component-catalog.json"),
            FileHash(root, "analysis/agent-handoff/visual-blueprint.json"),
            FileHash(root, "analysis/agent-handoff/page-compositions.json"),
            FileHash(root, "analysis/agent-handoff/evidence-manifest.json"),
            AgentHandoffContract.RequiredArtifacts.Select(artifact => artifact.RelativePath).ToArray(),
            BuildArtifactEntries(root),
            "Phase 4 consumers may read analysis/agent-handoff/* and schemas only; they must not reinterpret raw capture evidence or modify StorefrontBuilder generation without a separate approved plan.",
            readiness.Findings.Where(finding => finding.Severity == "blocking").Select(finding => $"{finding.Code}:{finding.Message}").ToArray());
        await WriteAsync(root, "manifest.json", manifest, cancellationToken);
        return manifest;
    }

    private static AgentHandoffFileManifest BuildAllowedFiles(string projectId, DateTimeOffset createdUtc, ReviewedPageCompositionsDocument compositions)
    {
        var paths = compositions.Compositions
            .SelectMany(composition => composition.SectionTree.SelectMany(Flatten))
            .Select(node => node.TargetFilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Where(path => !IsProtectedPath(path))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new AgentHandoffFileManifest(
            "1.0",
            "allowed-files",
            $"allowed-files-{projectId}",
            createdUtc,
            projectId,
            paths,
            [
                "Only generated storefront visual Razor/CSS/assets/copy/view-registration files are allowed.",
                "Do not add @page route ownership to generated visual files.",
                "Do not call Commerce Node Storefront APIs directly from browser code."
            ]);
    }

    private static AgentHandoffFileManifest BuildProtectedFiles(string projectId, DateTimeOffset createdUtc) =>
        new(
            "1.0",
            "protected-files",
            $"protected-files-{projectId}",
            createdUtc,
            projectId,
            [
                "BlazorShop.Storefront.Presentation",
                "BlazorShop.Storefront.Runtime",
                "BlazorShop.Storefront.Client",
                "BlazorShop.Storefront.V2",
                "BlazorShop.CommerceNode.API",
                "BlazorShop.ControlPlane.API",
                "StorefrontPackageVersions.props",
                "starter-generation.contract.yaml",
                "docs/storefront-analysis/generated-files.yaml"
            ],
            [
                "Protected paths are evidence/contract only.",
                "Phase 4 visual generation must fail if any target resolves into these paths."
            ]);

    private static AgentHandoffUnresolvedRegions BuildUnresolved(string projectId, DateTimeOffset createdUtc, GenerationReadinessReport readiness) =>
        new(
            "1.0",
            "unresolved-regions",
            $"unresolved-regions-{projectId}",
            createdUtc,
            projectId,
            readiness.Findings.Where(finding => finding.Severity == "blocking").Select(finding => $"{finding.Code}:{finding.Message}").ToArray(),
            readiness.Findings.Where(finding => finding.Severity == "warning").Select(finding => $"{finding.Code}:{finding.Message}").ToArray());

    private static IEnumerable<PageCompositionNode> Flatten(PageCompositionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static bool IsProtectedPath(string path) =>
        path.Contains("BlazorShop.Storefront.Presentation", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BlazorShop.Storefront.Runtime", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BlazorShop.Storefront.Client", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BlazorShop.Storefront.V2", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("CommerceNode", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("ControlPlane", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("StorefrontPackageVersions.props", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("starter-generation.contract.yaml", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteAsync<T>(string root, string fileName, T value, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, AgentHandoffContract.HandoffRoot.Replace('/', Path.DirectorySeparatorChar), fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, VisualJson.Options) + Environment.NewLine, cancellationToken);
    }

    private static async Task CopyAsync(string root, string source, string fileName, CancellationToken cancellationToken)
    {
        var sourcePath = Path.Combine(root, source.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            return;
        }

        var destinationPath = Path.Combine(root, AgentHandoffContract.HandoffRoot.Replace('/', Path.DirectorySeparatorChar), fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await File.WriteAllTextAsync(destinationPath, await File.ReadAllTextAsync(sourcePath, cancellationToken), cancellationToken);
    }

    private static IReadOnlyList<AgentHandoffArtifactEntry> BuildArtifactEntries(string root) =>
        AgentHandoffContract.RequiredArtifacts
            .Select(artifact => BuildArtifactEntry(root, artifact))
            .ToArray();

    private static AgentHandoffArtifactEntry BuildArtifactEntry(string root, RequiredHandoffArtifact artifact)
    {
        if (artifact.RelativePath is "analysis/agent-handoff/manifest.json" or "analysis/agent-handoff/handoff-readiness.json")
        {
            return new AgentHandoffArtifactEntry(artifact.RelativePath, artifact.ArtifactKind, "", 0, Required: true);
        }

        var path = Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (artifact.IsDirectory)
        {
            return new AgentHandoffArtifactEntry(artifact.RelativePath, artifact.ArtifactKind, "", Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count() : 0, Required: true);
        }

        return File.Exists(path)
            ? new AgentHandoffArtifactEntry(artifact.RelativePath, artifact.ArtifactKind, artifact.HashRequired ? FileHash(path) ?? "" : "", new FileInfo(path).Length, Required: true)
            : new AgentHandoffArtifactEntry(artifact.RelativePath, artifact.ArtifactKind, "", 0, Required: true);
    }

    private static string? ReadReviewBundleHash(string root)
    {
        var path = Path.Combine(root, "analysis", "resolved", "review-resolution-manifest.json");
        if (!File.Exists(path))
        {
            return null;
        }

        using var json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.TryGetProperty("decisionBundleHash", out var hash) ? hash.GetString() : null;
    }

    private static string? FileHash(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return FileHash(path);
    }

    private static string? FileHash(string path) =>
        File.Exists(path)
            ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
            : null;

    private static string WriteTask(
        VisualProject project,
        GenerationReadinessReport readiness,
        AgentHandoffFileManifest allowed,
        AgentHandoffFileManifest protectedFiles,
        ReviewedPageCompositionsDocument compositions,
        StorefrontPageContractsDocument? pageContracts)
    {
        var pages = pageContracts?.Pages ?? [];
        string Lines(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);
        return "# Storefront Visual Handoff Task" + Environment.NewLine + Environment.NewLine +
            "## Objective" + Environment.NewLine +
            $"Implement generated storefront visual files for `{project.ProjectId}` from reviewed handoff artifacts only. Readiness passed: `{readiness.Passed}`." + Environment.NewLine + Environment.NewLine +
            "## Inputs" + Environment.NewLine +
            Lines(AgentHandoffContract.RequiredArtifacts.Select(artifact => $"- `{artifact.RelativePath}`")) + Environment.NewLine + Environment.NewLine +
            "## Source of Truth Priority" + Environment.NewLine +
            "1. `handoff-readiness.json`" + Environment.NewLine +
            "2. `visual-blueprint.json`" + Environment.NewLine +
            "3. `storefront-pattern.json`" + Environment.NewLine +
            "4. `page-compositions.json`" + Environment.NewLine +
            "5. `allowed-files.json` / `protected-files.json`" + Environment.NewLine +
            "6. `design-tokens.json` / `visual-style.json`" + Environment.NewLine +
            "7. `screenshots/` / `section-screenshots/`" + Environment.NewLine + Environment.NewLine +
            "## Allowed File Operations" + Environment.NewLine +
            Lines(allowed.Paths.DefaultIfEmpty("(none)").Select(path => $"- `{path}`")) + Environment.NewLine + Environment.NewLine +
            "## Protected Files" + Environment.NewLine +
            Lines(protectedFiles.Paths.Select(path => $"- `{path}`")) + Environment.NewLine + Environment.NewLine +
            "## Required Page Slots" + Environment.NewLine +
            Lines(pages.Select(page => $"- `{page.PageId}`: {string.Join(", ", page.RequiredSlotIds.Select(slot => $"`{slot}`"))}")) + Environment.NewLine + Environment.NewLine +
            "## Optional Page Slots" + Environment.NewLine +
            Lines(pages.Select(page => $"- `{page.PageId}`: {string.Join(", ", page.OptionalSlotIds.Select(slot => $"`{slot}`"))}")) + Environment.NewLine + Environment.NewLine +
            "## Section Order" + Environment.NewLine +
            Lines(compositions.Compositions.Select(composition => $"- `{composition.PageId}`: {string.Join(" -> ", composition.SectionTree.Select(node => $"{node.NodeId} ({node.Role})"))}")) + Environment.NewLine + Environment.NewLine +
            "## Responsive Evidence" + Environment.NewLine +
            Lines(compositions.Compositions.Select(composition => $"- `{composition.PageId}`: {string.Join(", ", composition.ResponsiveTransformationRules.DefaultIfEmpty("no reviewed responsive override"))}")) + Environment.NewLine + Environment.NewLine +
            "## Interaction Evidence" + Environment.NewLine +
            "- Preserve action descriptors. Generated visual files may restyle or reposition but must not implement functional JavaScript, routes, BFF calls, SEO/media behavior, cart/checkout/account/auth logic, or payment logic." + Environment.NewLine + Environment.NewLine +
            "## Originality Restrictions" + Environment.NewLine +
            "- Screenshots, section crops, and reference assets are evidence-only and reference-only. Do not copy them into production-safe asset folders without explicit human review metadata." + Environment.NewLine + Environment.NewLine +
            "## Forbidden Behavior" + Environment.NewLine +
            "- No `@page` route declarations." + Environment.NewLine +
            "- No direct Commerce Node Storefront API browser calls." + Environment.NewLine +
            "- No BFF, SEO, media, cart, checkout, account/auth, payment, or Runtime transport reimplementation." + Environment.NewLine + Environment.NewLine +
            "## Unsupported Handling" + Environment.NewLine +
            Lines(readiness.Findings.Where(finding => finding.Severity == "blocking").Select(finding => $"- `{finding.Code}`: {finding.Message}").DefaultIfEmpty("- No blocking unsupported regions in generation readiness.")) + Environment.NewLine + Environment.NewLine +
            "## Validation Commands" + Environment.NewLine +
            "- `dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore`" + Environment.NewLine +
            "- `powershell -ExecutionPolicy Bypass -File scripts/qa/run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1`" + Environment.NewLine + Environment.NewLine +
            "## Stop Conditions" + Environment.NewLine +
            "- Stop if handoff readiness is false." + Environment.NewLine +
            "- Stop if a required page slot is missing." + Environment.NewLine +
            "- Stop if visual evidence is missing for a required major section." + Environment.NewLine +
            "- Stop if a target path is missing, outside allowed zones, or protected." + Environment.NewLine +
            "- Stop if unsupported critical pattern remains." + Environment.NewLine +
            "- Stop if implementation would require routes, BFF, SEO/media, cart/checkout/account/auth logic, payment logic, or functional JavaScript." + Environment.NewLine +
            "- StorefrontBuilder must not consume this package until a later approved Phase 4 cutover." + Environment.NewLine;
    }

    private static StorefrontPageContractsDocument? ReadPageContracts(string root)
    {
        var path = Path.Combine(root, "analysis", "storefront-pattern", "page-contracts.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<StorefrontPageContractsDocument>(File.ReadAllText(path), VisualJson.Options)
            : null;
    }

    private string? TryGetGitSha()
    {
        try
        {
            var headPath = Path.Combine(repoRoot, ".git", "HEAD");
            if (!File.Exists(headPath))
            {
                return null;
            }

            var head = File.ReadAllText(headPath).Trim();
            if (!head.StartsWith("ref:", StringComparison.Ordinal))
            {
                return head;
            }

            var refPath = Path.Combine(repoRoot, ".git", head[5..].Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
