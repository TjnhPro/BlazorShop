namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public sealed record VisualSchemaDefinition(
    string ArtifactKind,
    string SchemaVersion,
    IReadOnlyList<string> RequiredProperties);
