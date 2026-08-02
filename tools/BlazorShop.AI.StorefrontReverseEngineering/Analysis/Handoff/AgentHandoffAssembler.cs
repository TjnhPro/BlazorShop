using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
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
        await WritePageCompositionsAsync(root, project.ProjectId, createdUtc, compositions, cancellationToken);
        await WriteSemanticHandoffArtifactsAsync(root, project.ProjectId, createdUtc, cancellationToken);
        await CopyRequiredAsync(root, "analysis/storefront-pattern/storefront-pattern.json", "storefront-pattern.json", cancellationToken);
        await WritePresentationCatalogAsync(root, project.ProjectId, createdUtc, cancellationToken);
        await CopyRequiredAsync(root, "analysis/resolved/presentation-mappings.reviewed.json", "presentation-mappings.json", cancellationToken);
        await CopyRequiredAsync(root, "analysis/resolved/component-candidates.reviewed.json", "component-candidates.json", cancellationToken);
        await CopyRequiredAsync(root, "analysis/components/component-instances.json", "component-instances.json", cancellationToken);
        await WriteResponsiveHandoffArtifactAsync(root, project.ProjectId, createdUtc, compositions, cancellationToken);
        await WriteInteractionHandoffArtifactAsync(root, project.ProjectId, createdUtc, compositions, cancellationToken);
        await CopyRequiredAsync(root, "analysis/resolved/originality-restrictions.reviewed.json", "originality-restrictions.json", cancellationToken);
        await CopyRequiredAsync(root, "analysis/confidence/confidence-report.json", "confidence.json", cancellationToken);
        await WriteReviewResolutionAsync(root, project.ProjectId, createdUtc, cancellationToken);
        await WriteVisualBlueprintAsync(root, project.ProjectId, createdUtc, cancellationToken);
        await CopyRequiredAsync(root, "reports/generation-readiness.json", "generation-readiness.json", cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(root, AgentHandoffContract.HandoffRoot.Replace('/', Path.DirectorySeparatorChar), "task.md"),
            WriteTask(project, readiness, allowed, protectedFiles, compositions, ReadPageContracts(root)),
            cancellationToken);

        var artifactEntries = BuildArtifactEntries(root);
        var schemaRequirements = BuildSchemaRequirements();
        var packageHash = PortableHandoffPackageHasher.ComputePackageHash(
            artifactEntries.Select(ToPortableArtifactEntry),
            schemaRequirements);
        var manifest = new AgentHandoffManifest(
            "1.0",
            "agent-handoff-manifest",
            $"agent-handoff-{project.ProjectId}",
            createdUtc,
            project.ProjectId,
            AgentHandoffContract.PackageVersion,
            AgentHandoffContract.HandoffRoot,
            new AgentHandoffManifestDiagnostics(root, "diagnostics-only"),
            project.LatestRunId,
            TryGetGitSha(),
            "phase3d-agent-handoff-v2",
            readiness.Passed,
            ReadReviewBundleHash(root),
            FileHash(root, "analysis/agent-handoff/storefront-pattern.json"),
            FileHash(root, "analysis/agent-handoff/presentation-catalog.json"),
            FileHash(root, "analysis/agent-handoff/visual-blueprint.json"),
            FileHash(root, "analysis/agent-handoff/page-compositions.json"),
            FileHash(root, "analysis/agent-handoff/evidence-manifest.json"),
            AgentHandoffContract.RequiredArtifacts.Select(artifact => artifact.RelativePath).ToArray(),
            artifactEntries,
            schemaRequirements,
            BuildReferencePolicy(),
            packageHash,
            "External project paths are diagnostics-only and must not be required consumer dependencies.",
            "dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root <path> --schema-root <path>",
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

    private static async Task CopyRequiredAsync(string root, string source, string fileName, CancellationToken cancellationToken)
    {
        var sourcePath = Path.Combine(root, source.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-PORTABLE-001] Required handoff source artifact is missing. Problem: '{source}' was not found. Cause: Phase 4 consumer artifacts must be packaged locally. Fix: rerun the upstream workflow step that creates '{source}'.");
        }

        var destinationPath = Path.Combine(root, AgentHandoffContract.HandoffRoot.Replace('/', Path.DirectorySeparatorChar), fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await File.WriteAllTextAsync(destinationPath, await File.ReadAllTextAsync(sourcePath, cancellationToken), cancellationToken);
    }

    private static Task WritePageCompositionsAsync(
        string root,
        string projectId,
        DateTimeOffset createdUtc,
        ReviewedPageCompositionsDocument source,
        CancellationToken cancellationToken)
    {
        var provenance = source.Provenance.ReviewedInputArtifactPaths
            .Append(source.Provenance.ReviewResolutionManifestPath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(path => new HandoffDiagnosticReference(path, "diagnostics-only", ConsumerReadable: false))
            .ToArray();

        return WriteAsync(root, "page-compositions.json", new HandoffPageCompositions(
            "1.0",
            "agent-handoff-page-compositions",
            $"agent-handoff-page-compositions-{projectId}",
            createdUtc,
            projectId,
            provenance,
            source.Site,
            source.Pages,
            source.Compositions), cancellationToken);
    }

    private static Task WritePresentationCatalogAsync(string root, string projectId, DateTimeOffset createdUtc, CancellationToken cancellationToken)
    {
        var source = Read<PresentationComponentCatalog>(root, "presentation-catalog/presentation-component-catalog.json")
            ?? throw new InvalidOperationException("[SRE-HANDOFF-PORTABLE-006] Required presentation catalog is missing. Problem: 'presentation-catalog/presentation-component-catalog.json' was not found or did not parse. Cause: handoff presentation catalog must be packaged locally. Fix: rerun build-presentation-catalog.");

        var provenance = source.SourcePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(path => new HandoffDiagnosticReference(path, "diagnostics-only", ConsumerReadable: false))
            .ToArray();

        return WriteAsync(root, "presentation-catalog.json", new HandoffPresentationCatalog(
            "1.0",
            "agent-handoff-presentation-catalog",
            $"agent-handoff-presentation-catalog-{projectId}",
            createdUtc,
            source.Components,
            provenance), cancellationToken);
    }

    private static async Task WriteSemanticHandoffArtifactsAsync(string root, string projectId, DateTimeOffset createdUtc, CancellationToken cancellationToken)
    {
        var source = Read<SemanticTokenDocument>(root, "analysis/resolved/semantic-tokens.reviewed.json")
            ?? throw new InvalidOperationException("[SRE-HANDOFF-PORTABLE-002] Required reviewed semantic tokens are missing. Problem: 'analysis/resolved/semantic-tokens.reviewed.json' was not found or did not parse. Cause: design tokens and visual style must be packaged as handoff-local artifacts. Fix: rerun review resolution.");
        var sourceJson = ReadJsonObject(root, "analysis/resolved/semantic-tokens.reviewed.json")
            ?? throw new InvalidOperationException("[SRE-HANDOFF-PORTABLE-002] Required reviewed semantic tokens are missing. Problem: 'analysis/resolved/semantic-tokens.reviewed.json' was not found or did not parse. Cause: design tokens and visual style must be packaged as handoff-local artifacts. Fix: rerun review resolution.");
        var provenance = new[]
        {
            new HandoffDiagnosticReference(source.SourceRawTokensPath, "diagnostics-only", ConsumerReadable: false)
        };

        await WriteAsync(
            root,
            "design-tokens.json",
            BuildSemanticHandoffDocument(
                sourceJson,
                "agent-handoff-design-tokens",
                $"agent-handoff-design-tokens-{projectId}",
                createdUtc,
                projectId,
                provenance),
            cancellationToken);
        await WriteAsync(
            root,
            "visual-style.json",
            BuildSemanticHandoffDocument(
                sourceJson,
                "agent-handoff-visual-style",
                $"agent-handoff-visual-style-{projectId}",
                createdUtc,
                projectId,
                provenance),
            cancellationToken);
    }

    private static JsonObject? ReadJsonObject(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
    }

    private static JsonObject BuildSemanticHandoffDocument(
        JsonObject source,
        string artifactKind,
        string artifactId,
        DateTimeOffset createdUtc,
        string projectId,
        IReadOnlyList<HandoffDiagnosticReference> provenance)
    {
        var document = source.DeepClone().AsObject();
        document["schemaVersion"] = "1.0";
        document["artifactKind"] = artifactKind;
        document["artifactId"] = artifactId;
        document["createdUtc"] = JsonSerializer.SerializeToNode(createdUtc, VisualJson.Options);
        document["projectId"] = projectId;
        document.Remove("sourceRawTokensPath");
        document["diagnosticProvenance"] = JsonSerializer.SerializeToNode(provenance, VisualJson.Options);
        return document;
    }

    private static async Task WriteResponsiveHandoffArtifactAsync(
        string root,
        string projectId,
        DateTimeOffset createdUtc,
        ReviewedPageCompositionsDocument compositions,
        CancellationToken cancellationToken)
    {
        var pages = compositions.Pages
            .OrderBy(page => page.PageId, StringComparer.Ordinal)
            .Select(page =>
            {
                var source = Read<ResponsiveBehaviorDocument>(root, $"analysis/pages/{page.PageId}/responsive-behavior.json")
                    ?? throw new InvalidOperationException($"[SRE-HANDOFF-PORTABLE-003] Required responsive behavior is missing. Problem: page '{page.PageId}' has no responsive behavior artifact. Cause: Phase 4 responsive inputs must be site-level and handoff-local. Fix: rerun analyze-responsive-interactions.");
                return new HandoffResponsiveBehaviorPage(source.PageId, source.Sections, source.InferredBreakpointRanges, source.Issues);
            })
            .ToArray();

        await WriteAsync(root, "responsive-behavior.json", new HandoffResponsiveBehaviorDocument(
            "1.0",
            "agent-handoff-responsive-behavior",
            $"agent-handoff-responsive-behavior-{projectId}",
            createdUtc,
            projectId,
            "evidence-derived",
            pages), cancellationToken);
    }

    private static async Task WriteInteractionHandoffArtifactAsync(
        string root,
        string projectId,
        DateTimeOffset createdUtc,
        ReviewedPageCompositionsDocument compositions,
        CancellationToken cancellationToken)
    {
        var pages = compositions.Pages
            .OrderBy(page => page.PageId, StringComparer.Ordinal)
            .Select(page =>
            {
                var source = Read<InteractionModelDocument>(root, $"analysis/pages/{page.PageId}/interaction-model.json")
                    ?? throw new InvalidOperationException($"[SRE-HANDOFF-PORTABLE-004] Required interaction model is missing. Problem: page '{page.PageId}' has no interaction model artifact. Cause: Phase 4 interaction inputs must be site-level and handoff-local. Fix: rerun analyze-responsive-interactions.");
                return new HandoffInteractionModelPage(source.PageId, NormalizeInteractionPaths(source.Interactions), source.Issues);
            })
            .ToArray();

        await WriteAsync(root, "interaction-models.json", new HandoffInteractionModelsDocument(
            "1.0",
            "agent-handoff-interaction-models",
            $"agent-handoff-interaction-models-{projectId}",
            createdUtc,
            projectId,
            "evidence-derived",
            pages), cancellationToken);
    }

    private static Task WriteReviewResolutionAsync(string root, string projectId, DateTimeOffset createdUtc, CancellationToken cancellationToken)
    {
        var source = Read<ReviewResolutionManifest>(root, "analysis/resolved/review-resolution-manifest.json")
            ?? throw new InvalidOperationException("[SRE-HANDOFF-PORTABLE-007] Required review resolution is missing. Problem: 'analysis/resolved/review-resolution-manifest.json' was not found or did not parse. Cause: handoff review resolution must be packaged as a handoff-local artifact. Fix: resolve review decisions and rerun assemble-blueprint-v1.");

        var provenance = source.ResolvedArtifacts
            .Append("analysis/resolved/review-resolution-manifest.json")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(path => new HandoffDiagnosticReference(path, "diagnostics-only", ConsumerReadable: false))
            .ToArray();

        return WriteAsync(root, "review-resolution.json", new HandoffReviewResolution(
            "1.0",
            "agent-handoff-review-resolution",
            $"agent-handoff-review-resolution-{projectId}",
            createdUtc,
            projectId,
            source.SourceReviewQueueId,
            source.SourceReviewQueueHash,
            source.DecisionBundleHash,
            source.ResolvedItemCount,
            source.BlockingUnresolvedCount,
            source.ResolvedArtifacts,
            source.BlockedItems,
            provenance), cancellationToken);
    }

    private static IReadOnlyList<InteractionPattern> NormalizeInteractionPaths(IReadOnlyList<InteractionPattern> interactions) =>
        interactions
            .Select(interaction => interaction with
            {
                BeforeStylesPath = ToDiagnosticMarker(interaction.BeforeStylesPath),
                AfterStylesPath = ToDiagnosticMarker(interaction.AfterStylesPath)
            })
            .ToArray();

    private static string? ToDiagnosticMarker(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : $"diagnostics-only:{path}";

    private static async Task WriteVisualBlueprintAsync(string root, string projectId, DateTimeOffset createdUtc, CancellationToken cancellationToken)
    {
        var source = Read<VisualBlueprintV1>(root, "analysis/visual-blueprint.v1.reviewed.json")
            ?? throw new InvalidOperationException("[SRE-HANDOFF-PORTABLE-005] Required reviewed visual blueprint is missing. Problem: 'analysis/visual-blueprint.v1.reviewed.json' was not found or did not parse. Cause: handoff blueprint must be derived from the reviewed blueprint. Fix: resolve review blockers and rerun assemble-blueprint-v1.");
        var consumerReferences = ConsumerReferences();
        var sourceReferences = AllBlueprintReferences(source)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(reference => new HandoffDiagnosticReference(reference, "diagnostics-only", ConsumerReadable: false))
            .ToArray();
        await WriteAsync(root, "visual-blueprint.json", new HandoffVisualBlueprint(
            "1.0",
            "agent-handoff-visual-blueprint",
            $"agent-handoff-blueprint-{projectId}",
            createdUtc,
            projectId,
            consumerReferences,
            sourceReferences,
            new Dictionary<string, string>(StringComparer.Ordinal),
            source.Pages,
            source.GenerationRestrictions), cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> ConsumerReferences() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["manifest"] = "analysis/agent-handoff/manifest.json",
            ["task"] = "analysis/agent-handoff/task.md",
            ["allowedFiles"] = "analysis/agent-handoff/allowed-files.json",
            ["protectedFiles"] = "analysis/agent-handoff/protected-files.json",
            ["pageCompositions"] = "analysis/agent-handoff/page-compositions.json",
            ["designTokens"] = "analysis/agent-handoff/design-tokens.json",
            ["visualStyle"] = "analysis/agent-handoff/visual-style.json",
            ["storefrontPattern"] = "analysis/agent-handoff/storefront-pattern.json",
            ["presentationCatalog"] = "analysis/agent-handoff/presentation-catalog.json",
            ["presentationMappings"] = "analysis/agent-handoff/presentation-mappings.json",
            ["componentDefinitions"] = "analysis/agent-handoff/component-candidates.json",
            ["componentInstances"] = "analysis/agent-handoff/component-instances.json",
            ["responsiveBehavior"] = "analysis/agent-handoff/responsive-behavior.json",
            ["interactionModels"] = "analysis/agent-handoff/interaction-models.json",
            ["originalityRestrictions"] = "analysis/agent-handoff/originality-restrictions.json",
            ["confidence"] = "analysis/agent-handoff/confidence.json",
            ["reviewResolution"] = "analysis/agent-handoff/review-resolution.json",
            ["evidence"] = "analysis/agent-handoff/evidence-manifest.json",
            ["unresolvedRegions"] = "analysis/agent-handoff/unresolved-regions.json",
            ["generationReadiness"] = "analysis/agent-handoff/generation-readiness.json",
            ["handoffReadiness"] = "analysis/agent-handoff/handoff-readiness.json"
        };

    private static IEnumerable<string> AllBlueprintReferences(VisualBlueprintV1 blueprint)
    {
        foreach (var reference in blueprint.SourceProvenance) yield return reference;
        foreach (var reference in blueprint.PageArchetypes) yield return reference;
        yield return blueprint.Tokens;
        foreach (var reference in blueprint.Sections) yield return reference;
        foreach (var reference in blueprint.ResponsiveBehavior) yield return reference;
        foreach (var reference in blueprint.InteractionModels) yield return reference;
        yield return blueprint.ComponentDefinitions;
        yield return blueprint.ComponentInstances;
        foreach (var reference in blueprint.EcommerceRegions) yield return reference;
        yield return blueprint.PresentationMappings;
        yield return blueprint.UnsupportedPatterns;
        yield return blueprint.OriginalityRestrictions;
        yield return blueprint.Confidence;
        yield return blueprint.ReviewState;
    }

    private static IReadOnlyList<AgentHandoffArtifactEntry> BuildArtifactEntries(string root) =>
        AgentHandoffContract.RequiredArtifacts
            .Select(artifact => BuildArtifactEntry(root, artifact))
            .Concat(BuildDirectoryFileArtifactEntries(root))
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<AgentHandoffArtifactEntry> BuildDirectoryFileArtifactEntries(string root)
    {
        foreach (var artifact in AgentHandoffContract.RequiredArtifacts.Where(artifact => artifact.IsDirectory))
        {
            var directory = Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
                yield return new AgentHandoffArtifactEntry(
                    relative,
                    "agent-handoff-evidence-file",
                    FileHash(path) ?? "",
                    new FileInfo(path).Length,
                    Required: true,
                    "binary-file",
                    "1.0",
                    IncludeInPackageHash: true,
                    IsDirectory: false);
            }
        }
    }

    private static AgentHandoffArtifactEntry BuildArtifactEntry(string root, RequiredHandoffArtifact artifact)
    {
        if (artifact.RelativePath is "analysis/agent-handoff/manifest.json" or "analysis/agent-handoff/handoff-readiness.json")
        {
            return new AgentHandoffArtifactEntry(
                artifact.RelativePath,
                artifact.ArtifactKind,
                "",
                0,
                Required: true,
                artifact.SchemaName,
                "1.0",
                IncludeInPackageHash: false,
                IsDirectory: false);
        }

        var path = Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (artifact.IsDirectory)
        {
            return new AgentHandoffArtifactEntry(
                artifact.RelativePath,
                artifact.ArtifactKind,
                "",
                Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count() : 0,
                Required: true,
                artifact.SchemaName,
                "1.0",
                IncludeInPackageHash: false,
                IsDirectory: true);
        }

        return File.Exists(path)
            ? new AgentHandoffArtifactEntry(
                artifact.RelativePath,
                artifact.ArtifactKind,
                artifact.HashRequired ? FileHash(path) ?? "" : "",
                new FileInfo(path).Length,
                Required: true,
                artifact.SchemaName,
                "1.0",
                IncludeInPackageHash: artifact.HashRequired,
                IsDirectory: false)
            : new AgentHandoffArtifactEntry(
                artifact.RelativePath,
                artifact.ArtifactKind,
                "",
                0,
                Required: true,
                artifact.SchemaName,
                "1.0",
                IncludeInPackageHash: false,
                IsDirectory: false);
    }

    private static IReadOnlyList<PortableHandoffSchemaRequirement> BuildSchemaRequirements() =>
        AgentHandoffContract.RequiredSchemaKinds
            .Select(schema => new PortableHandoffSchemaRequirement(
                schema.SchemaKind,
                schema.ArtifactKind,
                schema.SchemaVersion,
                schema.SchemaFileName,
                schema.Sha256,
                schema.Required))
            .ToArray();

    private static PortableHandoffReferencePolicy BuildReferencePolicy() =>
        new(
            AgentHandoffContract.HandoffRoot,
            PortableHandoffReferenceCategories.All,
            RejectAbsoluteConsumerPaths: true,
            RejectConsumerPathEscape: true,
            RejectDraftConsumerReferences: true);

    private static PortableHandoffArtifactEntry ToPortableArtifactEntry(AgentHandoffArtifactEntry entry) =>
        new(
            entry.Path,
            entry.ArtifactKind,
            entry.SchemaKind,
            entry.SchemaVersion,
            entry.Sha256,
            entry.SizeBytes,
            entry.Required,
            entry.IncludeInPackageHash);

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

    private static T? Read<T>(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), VisualJson.Options)
            : default;
    }

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
            "- StorefrontBuilder must consume this package only through the approved Phase 4 preflight and generation plan; do not read raw captures, source analysis, Storefront V2 source, or reports as fallback input." + Environment.NewLine;
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
