using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;

public sealed class RawDesignTokenExtractor
{
    private const decimal NearDuplicatePixelThreshold = 1m;

    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public RawDesignTokenExtractor(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<RawDesignTokenDocument> ExtractAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var snapshot = await store.ReadJsonAsync<EvidenceSnapshot>(
            ArtifactPath.Create("analysis/evidence-snapshot.json"),
            "evidence-snapshot",
            cancellationToken);
        var configuration = await store.ReadJsonAsync<VisualProjectConfiguration>(
            ArtifactPath.Create("configuration.json"),
            "configuration",
            cancellationToken);
        var noiseSelectors = CapturePolicyDefaults.ResolveNoiseSelectors(configuration.CapturePolicy);
        var issues = new List<RawDesignTokenIssue>();
        var observations = new List<TokenObservation>();

        foreach (var page in snapshot.Pages)
        {
            foreach (var viewport in page.Viewports)
            {
                foreach (var element in viewport.Elements)
                {
                    if (ShouldIgnore(element, noiseSelectors))
                    {
                        issues.Add(new RawDesignTokenIssue(
                            "ignored-noise-or-hidden-element",
                            "info",
                            $"Ignored hidden or configured noise element '{element.Selector}'.",
                            element.EvidenceId));
                        continue;
                    }

                    CollectStyleObservations(page.PageId, viewport.ViewportId, element, observations);
                    CollectBoxObservations(page.PageId, viewport.ViewportId, element, observations);
                }
            }
        }
        await CollectInteractionObservationsAsync(root, store, snapshot, observations, issues, cancellationToken);

        var tokens = BuildTokens(observations).ToArray();
        var document = new RawDesignTokenDocument(
            "1.0",
            "raw-design-tokens",
            $"raw-design-tokens-{snapshot.ProjectId}",
            DateTimeOffset.UtcNow,
            snapshot.ProjectId,
            "analysis/evidence-snapshot.json",
            tokens,
            issues);
        var report = new RawDesignTokenFrequencyReport(
            "1.0",
            "raw-design-token-frequency-report",
            $"raw-design-token-frequency-{snapshot.ProjectId}",
            DateTimeOffset.UtcNow,
            snapshot.ProjectId,
            tokens.Length,
            tokens
                .GroupBy(token => token.Group, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new TokenGroupFrequency(group.Key, group.Count(), group.Sum(token => token.ProjectFrequency)))
                .ToArray(),
            issues);

        await store.WriteJsonAsync(
            ArtifactPath.Create("analysis/tokens/raw-design-tokens.json"),
            "raw-design-tokens",
            document,
            cancellationToken);
        await store.WriteJsonAsync(
            ArtifactPath.Create("analysis/tokens/token-frequency-report.json"),
            "raw-design-token-frequency-report",
            report,
            cancellationToken);
        return document;
    }

    private async Task CollectInteractionObservationsAsync(
        string root,
        FileSystemVisualArtifactStore store,
        EvidenceSnapshot snapshot,
        List<TokenObservation> observations,
        List<RawDesignTokenIssue> issues,
        CancellationToken cancellationToken)
    {
        foreach (var path in snapshot.SourceArtifactPaths.Where(path => path.EndsWith("interaction-evidence.json", StringComparison.Ordinal)))
        {
            InteractionEvidence evidence;
            try
            {
                evidence = await store.ReadJsonAsync<InteractionEvidence>(ArtifactPath.Create(path), "interaction-evidence", cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                issues.Add(new RawDesignTokenIssue("invalid-interaction-evidence", "blocking", exception.Message));
                continue;
            }

            if (evidence.ChangedElementEvidenceIds.Count == 0)
            {
                continue;
            }

            var stylesPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create(evidence.AfterStylesPath));
            if (!File.Exists(stylesPath))
            {
                issues.Add(new RawDesignTokenIssue(
                    "missing-interaction-style-artifact",
                    "blocking",
                    $"Interaction styles artifact is missing: {evidence.AfterStylesPath}"));
                continue;
            }

            var styles = JsonSerializer.Deserialize<IReadOnlyList<ComputedStyleSample>>(
                await File.ReadAllTextAsync(stylesPath, cancellationToken),
                VisualJson.Options) ?? [];
            var changed = evidence.ChangedElementEvidenceIds.ToHashSet(StringComparer.Ordinal);
            foreach (var style in styles.Where(style => !string.IsNullOrWhiteSpace(style.EvidenceId) && changed.Contains(style.EvidenceId)))
            {
                foreach (var property in style.Properties)
                {
                    var tokenGroup = ClassifyProperty("interaction", property.Key);
                    if (tokenGroup is null)
                    {
                        continue;
                    }

                    observations.Add(new TokenObservation(
                        tokenGroup,
                        property.Key,
                        property.Value,
                        NormalizeValue(property.Value),
                        evidence.PageId,
                        evidence.ViewportId,
                        style.EvidenceId!,
                        evidence.AfterStylesPath,
                        [$"interaction-state:{evidence.StateName}", "interaction-proven"]));
                }
            }
        }
    }

    private static void CollectStyleObservations(
        string pageId,
        string viewportId,
        EvidenceSnapshotElement element,
        List<TokenObservation> observations)
    {
        foreach (var group in element.StyleGroups)
        {
            foreach (var property in group.Value)
            {
                if (string.IsNullOrWhiteSpace(property.Value))
                {
                    continue;
                }

                var tokenGroup = ClassifyProperty(group.Key, property.Key);
                if (tokenGroup is null)
                {
                    continue;
                }

                observations.Add(new TokenObservation(
                    tokenGroup,
                    property.Key,
                    property.Value,
                    NormalizeValue(property.Value),
                    pageId,
                    viewportId,
                    element.EvidenceId,
                    element.SourceArtifactPath,
                    HintsFor(tokenGroup, property.Key, property.Value, element)));
            }
        }
    }

    private static void CollectBoxObservations(
        string pageId,
        string viewportId,
        EvidenceSnapshotElement element,
        List<TokenObservation> observations)
    {
        if (element.Box is not { Width: > 0, Height: > 0 } box)
        {
            return;
        }

        AddBox("width", box.Width);
        AddBox("height", box.Height);
        AddBox("aspect-ratio", Math.Round(box.Width / box.Height, 3));

        void AddBox(string property, decimal value)
        {
            var literal = value.ToString("0.###", CultureInfo.InvariantCulture);
            observations.Add(new TokenObservation(
                "layout",
                property,
                literal,
                NormalizeValue(literal),
                pageId,
                viewportId,
                element.EvidenceId,
                element.SourceArtifactPath,
                property == "aspect-ratio" ? ["aspect-ratio"] : ["box-metric"]));
        }
    }

    private static IReadOnlyList<RawDesignToken> BuildTokens(IReadOnlyList<TokenObservation> observations)
    {
        var outlierKeys = observations
            .GroupBy(observation => (observation.Group, observation.PropertyName))
            .SelectMany(group =>
            {
                var valueGroups = group.GroupBy(observation => observation.NormalizedValue, StringComparer.Ordinal).ToArray();
                return valueGroups.Length >= 4
                    ? valueGroups.Where(valueGroup => valueGroup.Count() == 1).Select(valueGroup => (group.Key.Group, group.Key.PropertyName, NormalizedValue: valueGroup.Key))
                    : [];
            })
            .ToHashSet();
        var nearDuplicateClusters = BuildNearDuplicateClusters(observations);

        return observations
            .GroupBy(observation => (observation.Group, observation.PropertyName, observation.NormalizedValue))
            .OrderBy(group => group.Key.Group, StringComparer.Ordinal)
            .ThenBy(group => group.Key.PropertyName, StringComparer.Ordinal)
            .ThenBy(group => group.Key.NormalizedValue, StringComparer.Ordinal)
            .Select(group =>
            {
                var key = group.Key;
                var sourceEvidenceIds = group.Select(observation => observation.EvidenceId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                var literalValues = group.Select(observation => observation.LiteralValue).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                return new RawDesignToken(
                    CreateTokenId(key.Group, key.PropertyName, key.NormalizedValue),
                    key.Group,
                    key.PropertyName,
                    key.NormalizedValue,
                    literalValues,
                    group.Count(),
                    group.GroupBy(observation => observation.PageId, StringComparer.Ordinal).Select(value => new TokenFrequency(value.Key, value.Count())).OrderBy(value => value.ScopeId, StringComparer.Ordinal).ToArray(),
                    group.GroupBy(observation => observation.ViewportId, StringComparer.Ordinal).Select(value => new TokenFrequency(value.Key, value.Count())).OrderBy(value => value.ScopeId, StringComparer.Ordinal).ToArray(),
                    sourceEvidenceIds,
                    group.Select(observation => observation.SourceArtifactPath).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                    outlierKeys.Contains((key.Group, key.PropertyName, key.NormalizedValue)),
                    nearDuplicateClusters.TryGetValue((key.Group, key.PropertyName, key.NormalizedValue), out var clusterId) ? clusterId : null,
                    group.SelectMany(observation => observation.Hints).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<(string Group, string PropertyName, string NormalizedValue), string> BuildNearDuplicateClusters(IReadOnlyList<TokenObservation> observations)
    {
        var clusters = new Dictionary<(string Group, string PropertyName, string NormalizedValue), string>();
        foreach (var propertyGroup in observations
            .GroupBy(observation => (observation.Group, observation.PropertyName)))
        {
            var numericValues = propertyGroup
                .Select(observation => (observation.NormalizedValue, Number: TryReadPixelValue(observation.NormalizedValue)))
                .Where(value => value.Number.HasValue)
                .Select(value => (value.NormalizedValue, Number: value.Number!.Value))
                .Distinct()
                .OrderBy(value => value.Number)
                .ToArray();
            var clusterIndex = 1;
            for (var index = 0; index < numericValues.Length - 1; index++)
            {
                if (Math.Abs(numericValues[index].Number - numericValues[index + 1].Number) > NearDuplicatePixelThreshold)
                {
                    continue;
                }

                var clusterId = $"near-{propertyGroup.Key.Group}-{propertyGroup.Key.PropertyName}-{clusterIndex++}";
                clusters[(propertyGroup.Key.Group, propertyGroup.Key.PropertyName, numericValues[index].NormalizedValue)] = clusterId;
                clusters[(propertyGroup.Key.Group, propertyGroup.Key.PropertyName, numericValues[index + 1].NormalizedValue)] = clusterId;
            }
        }

        return clusters;
    }

    private static bool ShouldIgnore(EvidenceSnapshotElement element, IReadOnlyList<string> noiseSelectors)
    {
        if (noiseSelectors.Any(selector => SelectorMatches(element.Selector, selector)))
        {
            return true;
        }

        var allStyles = element.StyleGroups.Values.SelectMany(group => group).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        return allStyles.TryGetValue("display", out var display) && string.Equals(display.Trim(), "none", StringComparison.OrdinalIgnoreCase) ||
               allStyles.TryGetValue("visibility", out var visibility) && string.Equals(visibility.Trim(), "hidden", StringComparison.OrdinalIgnoreCase) ||
               allStyles.TryGetValue("opacity", out var opacity) && decimal.TryParse(opacity, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value == 0m;
    }

    private static bool SelectorMatches(string selector, string noiseSelector) =>
        !string.IsNullOrWhiteSpace(noiseSelector) &&
        (selector.Contains(noiseSelector, StringComparison.OrdinalIgnoreCase) ||
         noiseSelector.StartsWith(".", StringComparison.Ordinal) && selector.Contains(noiseSelector[1..], StringComparison.OrdinalIgnoreCase) ||
         noiseSelector.StartsWith("#", StringComparison.Ordinal) && selector.Contains(noiseSelector[1..], StringComparison.OrdinalIgnoreCase));

    private static string? ClassifyProperty(string group, string propertyName)
    {
        var property = propertyName.ToLowerInvariant();
        if (group == "color" || property.Contains("color", StringComparison.Ordinal))
        {
            return "color";
        }

        if (group == "typography" || property is "font-family" or "font-size" or "font-weight" or "line-height" or "letter-spacing" or "text-transform")
        {
            return "typography";
        }

        if (property.Contains("margin", StringComparison.Ordinal) ||
            property.Contains("padding", StringComparison.Ordinal) ||
            property == "gap" ||
            property.EndsWith("-gap", StringComparison.Ordinal))
        {
            return "spacing";
        }

        if (group == "borderShadow" ||
            property.Contains("border", StringComparison.Ordinal) ||
            property.Contains("radius", StringComparison.Ordinal) ||
            property.Contains("shadow", StringComparison.Ordinal) ||
            property.Contains("outline", StringComparison.Ordinal))
        {
            return "shape";
        }

        if (group is "layout" or "positioning" ||
            property is "width" or "height" or "max-width" or "display" or "grid-template-columns" or "flex-direction" or "aspect-ratio" or "object-fit")
        {
            return "layout";
        }

        if (group == "motion" ||
            property.StartsWith("transition", StringComparison.Ordinal) ||
            property == "transform" ||
            property.Contains("animation", StringComparison.Ordinal))
        {
            return "motion";
        }

        return null;
    }

    private static IReadOnlyList<string> HintsFor(
        string group,
        string propertyName,
        string value,
        EvidenceSnapshotElement element)
    {
        var hints = new List<string>();
        if (group == "color" && propertyName.Contains("background", StringComparison.OrdinalIgnoreCase) && value.Contains("rgba", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add("overlay-color");
        }

        if (group == "color" && propertyName.Contains("color", StringComparison.OrdinalIgnoreCase) && element.Category.Contains("button", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add("accent-like");
        }

        if (group == "typography" && element.Category == "heading")
        {
            hints.Add("heading-candidate");
        }

        if (group == "typography" && element.Category is "section" or "semantic-landmark")
        {
            hints.Add("body-candidate");
        }

        if (group == "shape" && propertyName.Contains("outline", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add("focus-ring");
        }

        return hints;
    }

    private static string NormalizeValue(string value)
    {
        var trimmed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.Contains("rgb", StringComparison.OrdinalIgnoreCase)
            ? trimmed.ToLowerInvariant()
            : trimmed;
    }

    private static decimal? TryReadPixelValue(string normalizedValue)
    {
        var text = normalizedValue.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            ? normalizedValue[..^2]
            : normalizedValue;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string CreateTokenId(string group, string propertyName, string normalizedValue)
    {
        var source = $"{group}|{propertyName}|{normalizedValue}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..10].ToLowerInvariant();
        return $"raw-{group}-{Sanitize(propertyName)}-{hash}";
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        return builder.ToString().Trim('-');
    }

    private sealed record TokenObservation(
        string Group,
        string PropertyName,
        string LiteralValue,
        string NormalizedValue,
        string PageId,
        string ViewportId,
        string EvidenceId,
        string SourceArtifactPath,
        IReadOnlyList<string> Hints);
}
