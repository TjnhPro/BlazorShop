using System.Security.Cryptography;
using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
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
                    foreach (var node in MajorSectionsForPage(compositions, page.PageId))
                    {
                        sections.Add(await CropSectionAsync(projectRoot, page.PageId, viewport, node, cancellationToken));
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

    private static async Task<AgentHandoffSectionEvidence> CropSectionAsync(
        string projectRoot,
        string pageId,
        CaptureViewportManifest viewport,
        PageCompositionNode node,
        CancellationToken cancellationToken)
    {
        var sourceRelative = NormalizeProjectPath(viewport.ScreenshotPath);
        var sourcePath = Path.Combine(projectRoot, sourceRelative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-002] Section crop source is missing. Problem: '{sourceRelative}' was not found. Cause: capture evidence is incomplete. Fix: rerun capture before handoff packaging.");
        }

        using var image = new MagickImage(sourcePath);
        var bounds = ParseBounds(node.ViewportBoundingBoxes.Values.FirstOrDefault() ?? "");
        var x = Clamp((int)Math.Floor(bounds.X), 0, Math.Max((int)image.Width - 1, 0));
        var y = Clamp((int)Math.Floor(bounds.Y), 0, Math.Max((int)image.Height - 1, 0));
        var width = Clamp((int)Math.Ceiling(bounds.Width), 0, (int)image.Width - x);
        var height = Clamp((int)Math.Ceiling(bounds.Height), 0, (int)image.Height - y);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"[SRE-HANDOFF-EVIDENCE-003] Section crop bounds are invalid. Problem: page '{pageId}' section '{node.NodeId}' has bounds '{node.ViewportBoundingBoxes.Values.FirstOrDefault()}'. Cause: bounds clamp to an empty image. Fix: regenerate section evidence with non-empty bounds.");
        }

        image.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
        image.Page = new MagickGeometry(0, 0, (uint)width, (uint)height);

        var fileName = $"{SafePathSegment(node.NodeId)}.{SafePathSegment(viewport.ViewportId)}.png";
        var handoffRelative = $"{HandoffRoot}/section-screenshots/{SafePathSegment(pageId)}/{fileName}";
        var destination = Path.Combine(projectRoot, handoffRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await image.WriteAsync(destination, cancellationToken);
        return new AgentHandoffSectionEvidence(
            node.NodeId,
            InferSlot(node.Role),
            viewport.ViewportId,
            handoffRelative,
            sourceRelative,
            Sha256File(destination),
            $"x={x};y={y};width={width};height={height}",
            node.StateExpectations.Count == 0 ? "default" : string.Join(",", node.StateExpectations),
            ["evidence-only", "reference-only", "not-production-safe"]);
    }

    private static IEnumerable<PageCompositionNode> MajorSectionsForPage(ReviewedPageCompositionsDocument compositions, string pageId) =>
        compositions.Compositions
            .Where(composition => string.Equals(composition.PageId, pageId, StringComparison.Ordinal))
            .SelectMany(composition => composition.SectionTree.SelectMany(Flatten))
            .Where(IsMajorSection)
            .DistinctBy(node => node.NodeId);

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

    private static string NormalizeProjectPath(string path) => path.Replace('\\', '/').TrimStart('/');

    private static string Sha256File(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string SafePathSegment(string value)
    {
        var safe = new string(value.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
    }

    private static string? InferSlot(string role)
    {
        if (role.Contains("header", StringComparison.OrdinalIgnoreCase)) return "layout.header";
        if (role.Contains("footer", StringComparison.OrdinalIgnoreCase)) return "layout.footer";
        if (role.Contains("navigation", StringComparison.OrdinalIgnoreCase) || role.Contains("nav", StringComparison.OrdinalIgnoreCase)) return "layout.main-navigation";
        if (role.Contains("product card", StringComparison.OrdinalIgnoreCase)) return "catalog.product-card";
        if (role.Contains("gallery", StringComparison.OrdinalIgnoreCase)) return "product.gallery";
        if (role.Contains("information", StringComparison.OrdinalIgnoreCase)) return "product.information";
        if (role.Contains("purchase", StringComparison.OrdinalIgnoreCase)) return "product.purchase";
        if (role.Contains("cart", StringComparison.OrdinalIgnoreCase)) return "cart.page";
        if (role.Contains("checkout", StringComparison.OrdinalIgnoreCase)) return "checkout.page";
        if (role.Contains("account", StringComparison.OrdinalIgnoreCase)) return "account.shell";
        if (role.Contains("state", StringComparison.OrdinalIgnoreCase)) return "system.error";
        return null;
    }

    private static (decimal X, decimal Y, decimal Width, decimal Height) ParseBounds(string text)
    {
        var values = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => decimal.TryParse(parts[1], out var value) ? value : 0m, StringComparer.OrdinalIgnoreCase);
        return (
            values.GetValueOrDefault("x"),
            values.GetValueOrDefault("y"),
            values.GetValueOrDefault("width"),
            values.GetValueOrDefault("height"));
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
}
