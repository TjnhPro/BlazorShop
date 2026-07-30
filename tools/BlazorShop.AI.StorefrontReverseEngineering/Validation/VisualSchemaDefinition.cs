namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public sealed record VisualSchemaDefinition(
    string ArtifactKind,
    string SchemaVersion,
    IReadOnlyList<string> RequiredProperties,
    IReadOnlyList<string>? RequiredPaths = null,
    IReadOnlyList<string>? ArrayPaths = null,
    IReadOnlyList<VisualSchemaEnumRule>? EnumRules = null,
    IReadOnlyList<VisualSchemaNumericRule>? NumericRules = null);

public sealed record VisualSchemaEnumRule(
    string Path,
    IReadOnlyList<string> Values);

public sealed record VisualSchemaNumericRule(
    string Path,
    decimal? Minimum = null,
    decimal? Maximum = null);
