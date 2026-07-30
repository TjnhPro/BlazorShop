using System.Globalization;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;

public sealed class SemanticTokenNormalizer
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public SemanticTokenNormalizer(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<SemanticTokenDocument> NormalizeAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var raw = await store.ReadJsonAsync<RawDesignTokenDocument>(
            ArtifactPath.Create("analysis/tokens/raw-design-tokens.json"),
            "raw-design-tokens",
            cancellationToken);
        var conflicts = new List<SemanticTokenConflict>();
        var tokens = new List<SemanticToken>();

        AddColorTokens(raw, tokens, conflicts);
        AddTypographyTokens(raw, tokens, conflicts);
        AddSpacingAndShapeTokens(raw, tokens, conflicts);

        var pageOverrides = DetectPageOverrides(raw);
        var componentOverrides = Array.Empty<SemanticTokenOverride>();
        var reviewReasons = conflicts
            .Where(conflict => conflict.HumanReviewRequired)
            .Select(conflict => $"{conflict.Role}:{conflict.ReasonCode}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var document = new SemanticTokenDocument(
            "1.0",
            "semantic-tokens",
            $"semantic-tokens-{raw.ProjectId}",
            DateTimeOffset.UtcNow,
            raw.ProjectId,
            "analysis/tokens/raw-design-tokens.json",
            tokens.OrderBy(token => token.Group, StringComparer.Ordinal).ThenBy(token => token.Role, StringComparer.Ordinal).ToArray(),
            pageOverrides,
            componentOverrides,
            reviewReasons.Length > 0 || tokens.Any(token => token.HumanReviewRequired),
            reviewReasons);
        var conflictReport = new SemanticTokenConflictReport(
            "1.0",
            "semantic-token-conflicts",
            $"semantic-token-conflicts-{raw.ProjectId}",
            DateTimeOffset.UtcNow,
            raw.ProjectId,
            conflicts.OrderBy(conflict => conflict.Role, StringComparer.Ordinal).ToArray());

        await store.WriteJsonAsync(ArtifactPath.Create("analysis/tokens/semantic-tokens.draft.json"), "semantic-tokens", document, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/tokens/token-conflicts.json"), "semantic-token-conflicts", conflictReport, cancellationToken);
        return document;
    }

    private static void AddColorTokens(
        RawDesignTokenDocument raw,
        List<SemanticToken> tokens,
        List<SemanticTokenConflict> conflicts)
    {
        var colors = raw.Tokens.Where(token => token.Group == "color").ToArray();
        AssignFirst(tokens, "surface-page", "color", colors.Where(token => token.PropertyName == "background-color").OrderByDescending(token => token.ProjectFrequency), "background-frequency", 0.76m);
        AssignFirst(tokens, "surface-section", "color", colors.Where(token => token.PropertyName == "background-color").Skip(1).OrderByDescending(token => token.ProjectFrequency), "secondary-background", 0.58m);
        AssignFirst(tokens, "surface-card", "color", colors.Where(token => token.PropertyName == "background-color").OrderByDescending(token => token.SourceEvidenceIds.Count), "card-background-candidate", 0.55m);
        AssignFirst(tokens, "surface-elevated", "color", colors.Where(token => token.Hints.Contains("overlay-color") || token.PropertyName.Contains("shadow", StringComparison.OrdinalIgnoreCase)), "elevated-surface-candidate", 0.45m, reviewBelow: 0.5m);
        AssignFirst(tokens, "text-primary", "color", colors.Where(token => token.PropertyName == "color" && IsDark(token.NormalizedValue)).OrderByDescending(token => token.ProjectFrequency), "dark-text-frequency", 0.78m);
        AssignFirst(tokens, "text-secondary", "color", colors.Where(token => token.PropertyName == "color").Skip(1).OrderByDescending(token => token.ProjectFrequency), "secondary-text-candidate", 0.52m);
        AssignFirst(tokens, "text-muted", "color", colors.Where(token => token.PropertyName == "color").OrderBy(token => token.ProjectFrequency), "low-frequency-text-candidate", 0.45m, reviewBelow: 0.5m);
        AssignFirst(tokens, "text-inverse", "color", colors.Where(token => token.PropertyName == "color" && IsLight(token.NormalizedValue)), "light-text-candidate", 0.66m);
        AssignFirst(tokens, "border-default", "color", colors.Where(token => token.PropertyName.Contains("border", StringComparison.OrdinalIgnoreCase)).OrderByDescending(token => token.ProjectFrequency), "border-color-candidate", 0.62m);
        AssignFirst(tokens, "border-strong", "color", colors.Where(token => token.PropertyName.Contains("border", StringComparison.OrdinalIgnoreCase)).Skip(1), "secondary-border-color-candidate", 0.48m, reviewBelow: 0.5m);
        AssignAccent(tokens, colors, conflicts);
        AssignFirst(tokens, "state-success", "color", colors.Where(token => LooksGreen(token.NormalizedValue)), "green-state-candidate", 0.42m, reviewBelow: 0.5m);
        AssignFirst(tokens, "state-warning", "color", colors.Where(token => LooksYellow(token.NormalizedValue)), "yellow-state-candidate", 0.42m, reviewBelow: 0.5m);
        AssignFirst(tokens, "state-error", "color", colors.Where(token => LooksRed(token.NormalizedValue)), "red-state-candidate", 0.42m, reviewBelow: 0.5m);
        AssignFirst(tokens, "focus-ring", "color", colors.Where(token => token.Hints.Contains("focus-ring") || token.PropertyName.Contains("outline", StringComparison.OrdinalIgnoreCase)), "focus-ring-evidence", 0.64m);
        AssignFirst(tokens, "overlay", "color", colors.Where(token => token.Hints.Contains("overlay-color")), "overlay-evidence", 0.64m);
    }

    private static void AssignAccent(
        List<SemanticToken> tokens,
        IReadOnlyList<RawDesignToken> colors,
        List<SemanticTokenConflict> conflicts)
    {
        var candidates = colors
            .Where(token => token.Hints.Contains("accent-like") || token.Hints.Contains("interaction-proven") || IsSaturated(token.NormalizedValue))
            .OrderByDescending(token => token.ProjectFrequency)
            .ThenBy(token => token.TokenId, StringComparer.Ordinal)
            .Take(3)
            .ToArray();
        if (candidates.Length == 0)
        {
            return;
        }

        AddToken(tokens, "accent-primary", "color", [candidates[0]], "accent-candidate", candidates.Length > 1 ? 0.48m : 0.66m, candidates.Length > 1);
        if (candidates.Length > 1)
        {
            AddToken(tokens, "accent-secondary", "color", [candidates[1]], "secondary-accent-candidate", 0.46m, true);
            conflicts.Add(new SemanticTokenConflict(
                "accent-primary",
                "color",
                candidates.Select(token => token.TokenId).ToArray(),
                candidates.SelectMany(token => token.LiteralValues).Distinct(StringComparer.Ordinal).ToArray(),
                "ambiguous-accent-candidates",
                HumanReviewRequired: true));
        }
    }

    private static void AddTypographyTokens(RawDesignTokenDocument raw, List<SemanticToken> tokens, List<SemanticTokenConflict> conflicts)
    {
        var typography = raw.Tokens.Where(token => token.Group == "typography").ToArray();
        AssignFirst(tokens, "font-body", "typography", typography.Where(token => token.PropertyName == "font-family").OrderByDescending(token => token.ProjectFrequency), "font-family-frequency", 0.78m);
        AssignFirst(tokens, "font-heading", "typography", typography.Where(token => token.PropertyName == "font-family" && token.Hints.Contains("heading-candidate")).OrderByDescending(token => token.ProjectFrequency), "heading-font-candidate", 0.52m);
        var sizes = typography
            .Where(token => token.PropertyName == "font-size")
            .Select(token => (Token: token, Size: ReadNumber(token.NormalizedValue)))
            .Where(token => token.Size.HasValue)
            .OrderByDescending(token => token.Size!.Value)
            .Select(token => token.Token)
            .ToArray();
        var roles = new[] { "text-display", "text-h1", "text-h2", "text-h3", "text-body", "text-small", "text-label", "text-caption" };
        for (var index = 0; index < Math.Min(roles.Length, sizes.Length); index++)
        {
            AddToken(tokens, roles[index], "typography", [sizes[index]], "font-size-rank", index < 4 ? 0.62m : 0.54m);
        }
    }

    private static void AddSpacingAndShapeTokens(RawDesignTokenDocument raw, List<SemanticToken> tokens, List<SemanticTokenConflict> conflicts)
    {
        var spaces = raw.Tokens
            .Where(token => token.Group == "spacing")
            .Select(token => (Token: token, Size: ReadNumber(token.NormalizedValue)))
            .Where(token => token.Size.HasValue)
            .OrderBy(token => token.Size!.Value)
            .Select(token => token.Token)
            .DistinctBy(token => token.NormalizedValue)
            .ToArray();
        var spaceRoles = new[] { "space-1", "space-2", "space-3", "space-4", "space-5", "space-section", "space-container" };
        for (var index = 0; index < Math.Min(spaceRoles.Length, spaces.Length); index++)
        {
            AddToken(tokens, spaceRoles[index], "spacing", [spaces[index]], "spacing-scale-rank", 0.62m);
        }

        var radii = raw.Tokens
            .Where(token => token.Group == "shape" && token.PropertyName.Contains("radius", StringComparison.OrdinalIgnoreCase))
            .Select(token => (Token: token, Size: ReadNumber(token.NormalizedValue)))
            .Where(token => token.Size.HasValue)
            .OrderBy(token => token.Size!.Value)
            .Select(token => token.Token)
            .DistinctBy(token => token.NormalizedValue)
            .ToArray();
        var radiusRoles = new[] { "radius-small", "radius-medium", "radius-large", "radius-pill" };
        for (var index = 0; index < Math.Min(radiusRoles.Length, radii.Length); index++)
        {
            AddToken(tokens, radiusRoles[index], "shape", [radii[index]], "radius-scale-rank", 0.62m);
        }

        AssignFirst(tokens, "shadow-card", "shape", raw.Tokens.Where(token => token.Group == "shape" && token.PropertyName.Contains("shadow", StringComparison.OrdinalIgnoreCase)).OrderByDescending(token => token.ProjectFrequency), "shadow-frequency", 0.62m);
        AssignFirst(tokens, "shadow-elevated", "shape", raw.Tokens.Where(token => token.Group == "shape" && token.PropertyName.Contains("shadow", StringComparison.OrdinalIgnoreCase)).Skip(1), "secondary-shadow-candidate", 0.48m, reviewBelow: 0.5m);
    }

    private static IReadOnlyList<SemanticTokenOverride> DetectPageOverrides(RawDesignTokenDocument raw) =>
        raw.Tokens
            .Where(token => token.PageFrequencies.Count == 1 && token.ProjectFrequency == token.PageFrequencies[0].Count && token.ProjectFrequency <= 2)
            .Select(token => new SemanticTokenOverride(
                token.PageFrequencies[0].ScopeId,
                $"local-{token.Group}-{token.PropertyName}",
                [token.TokenId],
                token.SourceEvidenceIds,
                "page-local-low-frequency-token"))
            .Take(20)
            .ToArray();

    private static void AssignFirst(
        List<SemanticToken> tokens,
        string role,
        string group,
        IEnumerable<RawDesignToken> candidates,
        string reasonCode,
        decimal confidence,
        decimal reviewBelow = 0m)
    {
        var candidate = candidates.FirstOrDefault();
        if (candidate is not null)
        {
            AddToken(tokens, role, group, [candidate], reasonCode, confidence, confidence < reviewBelow);
        }
    }

    private static void AddToken(
        List<SemanticToken> tokens,
        string role,
        string group,
        IReadOnlyList<RawDesignToken> rawTokens,
        string reasonCode,
        decimal confidence,
        bool humanReviewRequired = false)
    {
        tokens.Add(new SemanticToken(
            role,
            group,
            rawTokens.SelectMany(token => token.LiteralValues).Distinct(StringComparer.Ordinal).ToArray(),
            rawTokens.Select(token => token.TokenId).Distinct(StringComparer.Ordinal).ToArray(),
            rawTokens.SelectMany(token => token.SourceEvidenceIds).Distinct(StringComparer.Ordinal).ToArray(),
            confidence,
            [reasonCode],
            humanReviewRequired));
    }

    private static decimal? ReadNumber(string value)
    {
        var text = value.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? value[..^2] : value;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    private static bool IsDark(string value) => TryReadHex(value, out var r, out var g, out var b) && r + g + b < 384;

    private static bool IsLight(string value) => TryReadHex(value, out var r, out var g, out var b) && r + g + b > 690;

    private static bool IsSaturated(string value) => TryReadHex(value, out var r, out var g, out var b) && Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 80;

    private static bool LooksGreen(string value) => TryReadHex(value, out var r, out var g, out var b) && g > r + 30 && g > b + 10;

    private static bool LooksYellow(string value) => TryReadHex(value, out var r, out var g, out var b) && r > 160 && g > 130 && b < 120;

    private static bool LooksRed(string value) => TryReadHex(value, out var r, out var g, out var b) && r > g + 40 && r > b + 40;

    private static bool TryReadHex(string value, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (!value.StartsWith("#", StringComparison.Ordinal) || value.Length != 7)
        {
            return false;
        }

        return int.TryParse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
               int.TryParse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
               int.TryParse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}
