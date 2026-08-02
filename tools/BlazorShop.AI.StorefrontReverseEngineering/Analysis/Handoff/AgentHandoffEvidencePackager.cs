using System.Security.Cryptography;
using System.Globalization;
using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using ImageMagick;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed class AgentHandoffEvidencePackager
{
    private const string HandoffRoot = "analysis/agent-handoff";

    public async Task<AgentHandoffEvidenceManifest> PackageAsync(
        string projectRoot,
        string projectId,
        DateTimeOffset createdUtc,
        ReviewedPageCompositionsDocument compositions,
        CancellationToken cancellationToken)
    {
        var mappings = Read<PresentationMappingsDocument>(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json")?.Mappings ?? [];
        var catalog = Read<PresentationComponentCatalog>(projectRoot, "presentation-catalog/presentation-component-catalog.json")?.Components ?? [];
        var contracts = Read<StorefrontPageContractsDocument>(projectRoot, "analysis/storefront-pattern/page-contracts.json")?.Pages ?? [];
        var slotResolver = new SectionSlotResolver(mappings, catalog);
        var pages = new List<AgentHandoffEvidencePage>();
        foreach (var page in compositions.Pages.OrderBy(page => page.PageId, StringComparer.Ordinal))
        {
            var captureManifest = ReadPageCaptureManifest(projectRoot, page.PageId);
            var screenshots = new List<AgentHandoffScreenshotEvidence>();
            var sections = new List<AgentHandoffSectionEvidence>();

            if (captureManifest is not null)
            {
                foreach (var viewportManifestPath in captureManifest.ViewportManifestPaths.Order(StringComparer.Ordinal))
                {
                    var viewport = ReadViewportManifest(projectRoot, viewportManifestPath);
                    if (viewport is null)
                    {
                        continue;
                    }

                    var screenshot = await CopyScreenshotAsync(projectRoot, page.PageId, viewport, cancellationToken);
                    screenshots.Add(screenshot);
                    foreach (var sectionSource in MajorSectionsForPage(compositions, page.PageId, contracts, slotResolver))
                    {
                        var section = await CropSectionAsync(projectRoot, page.PageId, viewport, sectionSource.Node, sectionSource.Slot, cancellationToken);
                        if (section is not null)
                        {
                            sections.Add(section);
                        }
                    }
                }
            }

            pages.Add(new AgentHandoffEvidencePage(
                page.PageId,
                page.SourceUrl,
                screenshots.OrderBy(screenshot => screenshot.ViewportId, StringComparer.Ordinal).ToArray(),
                sections.OrderBy(section => section.SectionId, StringComparer.Ordinal).ThenBy(section => section.ViewportId, StringComparer.Ordinal).ToArray()));
        }

        var manifest = new AgentHandoffEvidenceManifest(
            "1.0",
            "agent-handoff-evidence-manifest",
            $"agent-handoff-evidence-{projectId}",
            createdUtc,
            projectId,
            pages);
        var manifestPath = Path.Combine(projectRoot, HandoffRoot.Replace('/', Path.DirectorySeparatorChar), "evidence-manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, VisualJson.Options) + Environment.NewLine, cancellationToken);
        return manifest;
    }

    private static async Task<AgentHandoffScreenshotEvidence> CopyScreenshotAsync(
        string projectRoot,
        string pageId,
        CaptureViewportManifest viewport,
        CancellationToken cancellationToken)
    {
        var sourceRelative = NormalizeProjectPath(viewport.ScreenshotPath);
        var sourcePath = Path.Combine(projectRoot, sourceRelative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-001] Screenshot source is missing. Problem: '{sourceRelative}' was not found. Cause: capture evidence is incomplete. Fix: rerun capture before handoff packaging.");
        }

        var handoffRelative = $"{HandoffRoot}/screenshots/{SafePathSegment(pageId)}/{SafePathSegment(viewport.ViewportId)}.png";
        var destination = Path.Combine(projectRoot, handoffRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await File.WriteAllBytesAsync(destination, await File.ReadAllBytesAsync(sourcePath, cancellationToken), cancellationToken);
        return new AgentHandoffScreenshotEvidence(
            viewport.ViewportId,
            handoffRelative,
            sourceRelative,
            Sha256File(destination),
            viewport.ViewportWidth,
            viewport.ViewportHeight,
            viewport.DocumentWidth,
            viewport.DocumentHeight,
            "css-pixel",
            ["evidence-only", "reference-only", "not-production-safe"]);
    }

    private static async Task<AgentHandoffSectionEvidence?> CropSectionAsync(
        string projectRoot,
        string pageId,
        CaptureViewportManifest viewport,
        PageCompositionNode node,
        SectionSlotResolution slotResolution,
        CancellationToken cancellationToken)
    {
        var sourceRelative = NormalizeProjectPath(viewport.ScreenshotPath);
        var sourcePath = Path.Combine(projectRoot, sourceRelative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-002] Section crop source is missing. Problem: '{sourceRelative}' was not found. Cause: capture evidence is incomplete. Fix: rerun capture before handoff packaging.");
        }

        if (!node.ViewportBoundingBoxes.TryGetValue(viewport.ViewportId, out var viewportBounds))
        {
            if (IsHiddenInViewport(node, viewport.ViewportId))
            {
                return null;
            }

            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-003] missing-section-viewport-bounds. Problem: page '{pageId}' section '{node.NodeId}' has no bounds for viewport '{viewport.ViewportId}'. Cause: section evidence was not correlated for this viewport. Fix: regenerate analysis with viewport-specific section bounds.");
        }

        if (!TryParseBounds(viewportBounds, out var bounds) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-004] invalid-section-viewport-bounds. Problem: page '{pageId}' section '{node.NodeId}' viewport '{viewport.ViewportId}' has bounds '{viewportBounds}'. Cause: bounds are missing, malformed, or zero sized. Fix: regenerate section evidence with numeric x/y/width/height for this viewport.");
        }

        using var image = new MagickImage(sourcePath);
        var x = Clamp((int)Math.Floor(bounds.X), 0, Math.Max((int)image.Width - 1, 0));
        var y = Clamp((int)Math.Floor(bounds.Y), 0, Math.Max((int)image.Height - 1, 0));
        var width = Clamp((int)Math.Ceiling(bounds.Width), 0, (int)image.Width - x);
        var height = Clamp((int)Math.Ceiling(bounds.Height), 0, (int)image.Height - y);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-005] section-crop-out-of-range. Problem: page '{pageId}' section '{node.NodeId}' viewport '{viewport.ViewportId}' bounds '{viewportBounds}' are outside screenshot '{sourceRelative}'. Cause: bounds clamp to an empty image. Fix: regenerate section evidence for the same screenshot and viewport.");
        }

        image.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
        image.Page = new MagickGeometry(0, 0, (uint)width, (uint)height);
        image.Strip();

        var fileName = $"{SafePathSegment(node.NodeId)}.{SafePathSegment(viewport.ViewportId)}.png";
        var handoffRelative = $"{HandoffRoot}/section-screenshots/{SafePathSegment(pageId)}/{fileName}";
        var destination = Path.Combine(projectRoot, handoffRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await image.WriteAsync(destination, cancellationToken);
        return new AgentHandoffSectionEvidence(
            node.NodeId,
            slotResolution.StarterSlotId,
            slotResolution.SlotSource,
            slotResolution.MappingId,
            slotResolution.SuggestedSlotId,
            viewport.ViewportId,
            handoffRelative,
            sourceRelative,
            Sha256File(destination),
            $"x={x};y={y};width={width};height={height}",
            node.StateExpectations.Count == 0 ? "default" : string.Join(",", node.StateExpectations),
            ["evidence-only", "reference-only", "not-production-safe"]);
    }

    private static IEnumerable<SectionEvidenceSource> MajorSectionsForPage(
        ReviewedPageCompositionsDocument compositions,
        string pageId,
        IReadOnlyList<StorefrontPageContract> contracts,
        SectionSlotResolver slotResolver) =>
        compositions.Compositions
            .Where(composition => string.Equals(composition.PageId, pageId, StringComparison.Ordinal))
            .SelectMany(composition =>
            {
                var contract = MatchContract(contracts, composition.PageId, composition.PageArchetype);
                return composition.SectionTree
                    .SelectMany(Flatten)
                    .Where(IsMajorSection)
                    .Select(node => new SectionEvidenceSource(node, slotResolver.Resolve(composition, node, contract)));
            })
            .DistinctBy(source => source.Node.NodeId);

    private static IEnumerable<PageCompositionNode> Flatten(PageCompositionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static bool IsMajorSection(PageCompositionNode node)
    {
        var role = node.Role;
        return role.Contains("header", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("navigation", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("hero", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("product grid", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("product card", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("gallery", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("information", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("purchase", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("cart", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("checkout", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("account", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("footer", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("state", StringComparison.OrdinalIgnoreCase) ||
            node.TargetFilePath is not null;
    }

    private static PageCaptureManifest? ReadPageCaptureManifest(string projectRoot, string pageId)
    {
        var path = Path.Combine(projectRoot, "captures", pageId, "capture-manifest.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<PageCaptureManifest>(File.ReadAllText(path), VisualJson.Options)
            : null;
    }

    private static CaptureViewportManifest? ReadViewportManifest(string projectRoot, string relativePath)
    {
        var path = Path.Combine(projectRoot, NormalizeProjectPath(relativePath).Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? JsonSerializer.Deserialize<CaptureViewportManifest>(File.ReadAllText(path), VisualJson.Options)
            : null;
    }

    private static T? Read<T>(string projectRoot, string relativePath)
    {
        var path = Path.Combine(projectRoot, NormalizeProjectPath(relativePath).Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), VisualJson.Options)
            : default;
    }

    private static string NormalizeProjectPath(string path) => path.Replace('\\', '/').TrimStart('/');

    private static string Sha256File(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string SafePathSegment(string value)
    {
        var safe = new string(value.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
    }

    private static bool IsHiddenInViewport(PageCompositionNode node, string viewportId) =>
        node.ResponsiveTransformationRules.Any(rule =>
            rule.Contains("hidden", StringComparison.OrdinalIgnoreCase) &&
            rule.Contains(viewportId, StringComparison.OrdinalIgnoreCase));

    private static bool TryParseBounds(string text, out (decimal X, decimal Y, decimal Width, decimal Height) bounds)
    {
        bounds = default;
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pieces.Length != 2 || !decimal.TryParse(pieces[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            values[pieces[0]] = value;
        }

        if (!values.TryGetValue("x", out var x) ||
            !values.TryGetValue("y", out var y) ||
            !values.TryGetValue("width", out var width) ||
            !values.TryGetValue("height", out var height))
        {
            return false;
        }

        bounds = (x, y, width, height);
        return true;
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private static StorefrontPageContract? MatchContract(IReadOnlyList<StorefrontPageContract> contracts, string pageId, string pageArchetype) =>
        contracts.FirstOrDefault(contract =>
            string.Equals(contract.PageId, pageId, StringComparison.Ordinal) ||
            string.Equals(contract.StablePageArchetype, pageArchetype, StringComparison.Ordinal));

    private sealed record SectionEvidenceSource(
        PageCompositionNode Node,
        SectionSlotResolution Slot);
}
