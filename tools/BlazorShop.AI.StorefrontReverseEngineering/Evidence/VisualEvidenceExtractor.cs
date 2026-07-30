using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
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
        BrowserCaptureResult capture,
        string? runId,
        EvidenceExtractionOptions options,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var relativeRoot = $"captures/{session.PageId}/{viewportId}";

        var index = BuildElementIndex(session, viewportId, capture, runId, options);
        var assets = BuildAssetInventory(session, viewportId, capture, runId);

        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/element-evidence-index.json"), "computed-style-evidence", index, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/asset-inventory.normalized.json"), "asset-inventory", assets, cancellationToken);

        var pageManifest = new PageCaptureManifest(
            "1.0",
            "capture-manifest",
            $"capture-page-{session.ProjectId}-{session.PageId}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            runId,
            [$"{relativeRoot}/manifest.json"],
            [$"{relativeRoot}/element-evidence-index.json", $"{relativeRoot}/asset-inventory.normalized.json"]);

        await store.WriteJsonAsync(ArtifactPath.Create($"captures/{session.PageId}/capture-manifest.json"), "capture-manifest", pageManifest, cancellationToken);
        return index;
    }

    public void ValidateReferencedFiles(string projectRoot, PageCaptureManifest manifest)
    {
        var root = resolver.ResolveRoot(projectRoot);
        foreach (var path in manifest.ViewportManifestPaths.Concat(manifest.EvidenceArtifactPaths))
        {
            var fullPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create(path));
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"[SRE-EVIDENCE-001] Referenced evidence file is missing. Problem: '{path}' does not exist under the project root. Cause: capture/evidence manifests must only reference persisted files. Fix: rerun capture or evidence extraction.");
            }
        }
    }

    private static ElementEvidenceIndex BuildElementIndex(
        BrowserPageSession session,
        string viewportId,
        BrowserCaptureResult capture,
        string? runId,
        EvidenceExtractionOptions options)
    {
        var selectors = SelectEvidenceSelectors(capture.DomHtml)
            .Take(options.MaximumElements)
            .ToArray();
        var boxes = capture.Boxes.ToDictionary(box => box.Selector, StringComparer.Ordinal);
        var styles = capture.Styles.ToDictionary(style => style.Selector, StringComparer.Ordinal);

        var elements = selectors.Select((selector, index) =>
        {
            boxes.TryGetValue(selector.Selector, out var box);
            styles.TryGetValue(selector.Selector, out var style);
            return new ElementEvidenceItem(
                $"ev-{viewportId}-{index + 1:000}",
                selector.Selector,
                selector.Category,
                selector.TextSnippet,
                GroupStyles(style?.Properties ?? new Dictionary<string, string>()),
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
            elements);
    }

    private static AssetInventoryEvidence BuildAssetInventory(
        BrowserPageSession session,
        string viewportId,
        BrowserCaptureResult capture,
        string? runId)
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
            capture.Assets.Select((asset, index) => new AssetEvidenceItem(
                $"asset-{viewportId}-{index + 1:000}",
                asset.Url,
                asset.MediaType,
                asset.Width,
                asset.Height,
                asset.SourceElement,
                ReferenceOnly: true)).ToArray());
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
