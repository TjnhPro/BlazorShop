using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Evidence;

public sealed partial class VisualEvidenceExtractor
{
    private static readonly HashSet<string> AllowedStyleProperties = new(StringComparer.Ordinal)
    {
        "font-family",
        "font-size",
        "font-weight",
        "line-height",
        "color",
        "background",
        "background-color",
        "border",
        "border-radius",
        "box-shadow",
        "display",
        "grid-template-columns",
        "flex-direction",
        "gap",
        "position",
        "top",
        "left",
        "transform",
        "transition"
    };

    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public VisualEvidenceExtractor(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<ElementEvidenceIndex> WriteViewportEvidenceAsync(
        string projectRoot,
        BrowserPageSession session,
        string viewportId,
        CapturedViewportResult captured,
        EvidenceExtractionOptions options,
        CancellationToken cancellationToken)
    {
        return await WriteViewportEvidenceAsync(
            projectRoot,
            session,
            viewportId,
            captured.Capture,
            captured.RunId,
            options,
            cancellationToken,
            captured.CaptureCorrelationId);
    }

    public async Task<ElementEvidenceIndex> WriteViewportEvidenceAsync(
        string projectRoot,
        BrowserPageSession session,
        string viewportId,
        BrowserCaptureResult capture,
        string? runId,
        EvidenceExtractionOptions options,
        CancellationToken cancellationToken,
        string? captureCorrelationId = null)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var relativeRoot = $"captures/{session.PageId}/{viewportId}";

        var correlationId = captureCorrelationId
            ?? capture.CaptureCorrelationId
            ?? $"capture-{session.ProjectId}-{session.PageId}-{viewportId}-{Guid.NewGuid():N}";
        var index = BuildElementIndex(session, viewportId, capture, runId, options, correlationId);
        var assets = BuildAssetInventory(session, viewportId, capture, runId, correlationId);

        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/element-evidence-index.json"), "computed-style-evidence", index, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/asset-inventory.normalized.json"), "asset-inventory", assets, cancellationToken);

        var pageManifest = MergePageManifest(
            root,
            session,
            runId,
            viewportId,
            $"{relativeRoot}/manifest.json",
            $"{relativeRoot}/capture-quality-report.json",
            [$"{relativeRoot}/element-evidence-index.json", $"{relativeRoot}/asset-inventory.normalized.json"],
            correlationId);

        await store.WriteJsonAsync(ArtifactPath.Create($"captures/{session.PageId}/capture-manifest.json"), "page-capture-manifest", pageManifest, cancellationToken);
        return index;
    }

    private PageCaptureManifest MergePageManifest(
        string root,
        BrowserPageSession session,
        string? runId,
        string viewportId,
        string viewportManifestPath,
        string qualityReportPath,
        IReadOnlyList<string> evidencePaths,
        string? captureCorrelationId)
    {
        var manifestPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create($"captures/{session.PageId}/capture-manifest.json"));
        PageCaptureManifest? existing = null;
        if (File.Exists(manifestPath))
        {
            var json = File.ReadAllText(manifestPath);
            existing = System.Text.Json.JsonSerializer.Deserialize<PageCaptureManifest>(json, VisualJson.Options);
        }

        var viewportPaths = (existing?.ViewportManifestPaths ?? [])
            .Where(path => !path.Equals(viewportManifestPath, StringComparison.Ordinal))
            .Append(viewportManifestPath)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var evidenceArtifactPaths = (existing?.EvidenceArtifactPaths ?? [])
            .Concat(evidencePaths)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var qualityPaths = new Dictionary<string, string>(existing?.QualityReportPaths ?? new Dictionary<string, string>(), StringComparer.Ordinal)
        {
            [viewportId] = qualityReportPath
        };
        var correlationIds = new Dictionary<string, string>(existing?.CaptureCorrelationIds ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(captureCorrelationId))
        {
            correlationIds[viewportId] = captureCorrelationId!;
        }

        return new PageCaptureManifest(
            "1.0",
            "page-capture-manifest",
            $"capture-page-{session.ProjectId}-{session.PageId}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            runId,
            viewportPaths,
            evidenceArtifactPaths,
            qualityPaths,
            correlationIds);
    }

    public void ValidateReferencedFiles(string projectRoot, PageCaptureManifest manifest)
    {
        var root = resolver.ResolveRoot(projectRoot);
        foreach (var path in manifest.ViewportManifestPaths
                     .Concat(manifest.EvidenceArtifactPaths)
                     .Concat(manifest.QualityReportPaths?.Values ?? []))
        {
            var fullPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create(path));
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"[SRE-EVIDENCE-001] Referenced evidence file is missing. Problem: '{path}' does not exist under the project root. Cause: capture/evidence manifests must only reference persisted files. Fix: rerun capture or evidence extraction.");
            }
        }

        foreach (var pair in manifest.CaptureCorrelationIds ?? new Dictionary<string, string>())
        {
            var viewportManifestPath = manifest.ViewportManifestPaths.FirstOrDefault(path => path.Contains($"/{pair.Key}/", StringComparison.Ordinal));
            if (viewportManifestPath is not null)
            {
                var viewportManifest = ReadJson<CaptureViewportManifest>(root, viewportManifestPath);
                if (!string.Equals(viewportManifest.CaptureCorrelationId, pair.Value, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"[SRE-EVIDENCE-002] Viewport manifest correlation mismatch. Problem: '{viewportManifestPath}' does not match page manifest correlation for viewport '{pair.Key}'. Cause: raw and normalized artifacts came from different capture snapshots. Fix: rerun capture and evidence extraction for the viewport.");
                }
            }

            foreach (var evidencePath in manifest.EvidenceArtifactPaths.Where(path => path.Contains($"/{pair.Key}/", StringComparison.Ordinal)))
            {
                var correlationId = ReadCorrelationId(root, evidencePath);
                if (!string.Equals(correlationId, pair.Value, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"[SRE-EVIDENCE-003] Normalized evidence correlation mismatch. Problem: '{evidencePath}' does not match page manifest correlation for viewport '{pair.Key}'. Cause: raw and normalized artifacts came from different capture snapshots. Fix: rerun capture and evidence extraction for the viewport.");
                }
            }
        }
    }

    private TArtifact ReadJson<TArtifact>(string root, string path)
    {
        var fullPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create(path));
        var json = File.ReadAllText(fullPath);
        return System.Text.Json.JsonSerializer.Deserialize<TArtifact>(json, VisualJson.Options)
            ?? throw new InvalidOperationException($"[SRE-EVIDENCE-004] Artifact could not be parsed. Problem: '{path}' was empty or invalid JSON. Cause: capture artifact is corrupted. Fix: rerun capture.");
    }

    private string? ReadCorrelationId(string root, string path)
    {
        if (path.EndsWith("element-evidence-index.json", StringComparison.Ordinal))
        {
            return ReadJson<ElementEvidenceIndex>(root, path).CaptureCorrelationId;
        }

        if (path.EndsWith("asset-inventory.normalized.json", StringComparison.Ordinal))
        {
            return ReadJson<AssetInventoryEvidence>(root, path).CaptureCorrelationId;
        }

        return null;
    }

    private static ElementEvidenceIndex BuildElementIndex(
        BrowserPageSession session,
        string viewportId,
        BrowserCaptureResult capture,
        string? runId,
        EvidenceExtractionOptions options,
        string? captureCorrelationId)
    {
        var styles = capture.Styles
            .Where(style => !string.IsNullOrWhiteSpace(style.Selector))
            .Take(options.MaximumElements)
            .ToArray();
        var boxesByEvidenceId = capture.Boxes
            .Where(box => !string.IsNullOrWhiteSpace(box.EvidenceId))
            .ToDictionary(box => box.EvidenceId!, StringComparer.Ordinal);
        var boxesBySelector = capture.Boxes
            .GroupBy(box => box.Selector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var elements = styles.Select((style, index) =>
        {
            var evidenceId = string.IsNullOrWhiteSpace(style.EvidenceId)
                ? $"ev-{viewportId}-{index + 1:000}"
                : style.EvidenceId!;
            if (!boxesByEvidenceId.TryGetValue(evidenceId, out var box))
            {
                boxesBySelector.TryGetValue(style.Selector, out box);
            }

            return new ElementEvidenceItem(
                evidenceId,
                style.Selector,
                ClassifySelector(style.Selector),
                style.Properties.TryGetValue("text-snippet", out var text) ? text : null,
                GroupStyles(style.Properties),
                box is null ? null : new ElementBox(box.X, box.Y, box.Width, box.Height));
        }).ToArray();

        return new ElementEvidenceIndex(
            "1.0",
            "computed-style-evidence",
            $"element-evidence-{session.ProjectId}-{session.PageId}-{viewportId}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            viewportId,
            runId,
            elements,
            captureCorrelationId);
    }

    private static AssetInventoryEvidence BuildAssetInventory(
        BrowserPageSession session,
        string viewportId,
        BrowserCaptureResult capture,
        string? runId,
        string? captureCorrelationId)
    {
        return new AssetInventoryEvidence(
            "1.0",
            "asset-inventory",
            $"asset-inventory-{session.ProjectId}-{session.PageId}-{viewportId}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            viewportId,
            runId,
            capture.Assets.Take(80).Select((asset, index) => new AssetEvidenceItem(
                string.IsNullOrWhiteSpace(asset.EvidenceId) ? $"asset-{viewportId}-{index + 1:000}" : asset.EvidenceId!,
                asset.Url,
                asset.MediaType,
                asset.Width,
                asset.Height,
                asset.SourceElement,
                ReferenceOnly: true)).ToArray(),
            captureCorrelationId);
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GroupStyles(IReadOnlyDictionary<string, string> properties)
    {
        var allowlisted = properties
            .Where(pair => AllowedStyleProperties.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        return new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["typography"] = Pick(allowlisted, "font-family", "font-size", "font-weight", "line-height"),
            ["color"] = Pick(allowlisted, "color", "background", "background-color"),
            ["borderShadow"] = Pick(allowlisted, "border", "border-radius", "box-shadow"),
            ["layout"] = Pick(allowlisted, "display", "grid-template-columns", "flex-direction", "gap"),
            ["positioning"] = Pick(allowlisted, "position", "top", "left"),
            ["motion"] = Pick(allowlisted, "transform", "transition")
        };
    }

    private static IReadOnlyDictionary<string, string> Pick(IReadOnlyDictionary<string, string> properties, params string[] names) =>
        names.Where(properties.ContainsKey).ToDictionary(name => name, name => properties[name], StringComparer.Ordinal);

    private static string ClassifySelector(string selector)
    {
        if (selector.Contains("product", StringComparison.OrdinalIgnoreCase) ||
            selector.Contains("card", StringComparison.OrdinalIgnoreCase))
        {
            return "product-card-candidate";
        }

        if (selector.StartsWith("h1", StringComparison.Ordinal) ||
            selector.StartsWith("h2", StringComparison.Ordinal) ||
            selector.StartsWith("h3", StringComparison.Ordinal) ||
            selector.StartsWith("h4", StringComparison.Ordinal) ||
            selector.StartsWith("h5", StringComparison.Ordinal) ||
            selector.StartsWith("h6", StringComparison.Ordinal))
        {
            return "heading";
        }

        if (selector.StartsWith("header", StringComparison.Ordinal) ||
            selector.StartsWith("main", StringComparison.Ordinal) ||
            selector.StartsWith("footer", StringComparison.Ordinal) ||
            selector.StartsWith("nav", StringComparison.Ordinal))
        {
            return "semantic-landmark";
        }

        if (selector.StartsWith("button", StringComparison.Ordinal))
        {
            return "button";
        }

        if (selector.StartsWith("a", StringComparison.Ordinal))
        {
            return "link";
        }

        if (selector.StartsWith("img", StringComparison.Ordinal))
        {
            return "image";
        }

        if (selector.StartsWith("section", StringComparison.Ordinal))
        {
            return "section";
        }

        return "element";
    }

    private static IEnumerable<(string Selector, string Category, string? TextSnippet)> SelectEvidenceSelectors(string html)
    {
        var selectors = new List<(string, string, string?)>
        {
            ("header", "semantic-landmark", null),
            ("main", "semantic-landmark", null),
            ("footer", "semantic-landmark", null),
            ("section", "section", null),
            ("article", "article", null),
            ("button", "button", null),
            ("a", "link", null),
            ("input", "input", null),
            ("img", "image", null),
            (".product-card", "product-card-candidate", null)
        };

        selectors.AddRange(HeadingRegex().Matches(html).Select(match => ($"h{match.Groups["level"].Value}", "heading", (string?)Strip(match.Groups["text"].Value))));
        return selectors.DistinctBy(selector => selector.Item1 + selector.Item2);
    }

    private static string Strip(string value) => Regex.Replace(value, "<.*?>", string.Empty).Trim();

    [GeneratedRegex("<h(?<level>[1-6])[^>]*>(?<text>.*?)</h[1-6]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingRegex();
}
