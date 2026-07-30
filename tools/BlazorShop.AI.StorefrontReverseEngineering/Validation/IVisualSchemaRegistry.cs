namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public interface IVisualSchemaRegistry
{
    IReadOnlyCollection<VisualSchemaDefinition> Schemas { get; }

    VisualSchemaDefinition GetRequired(string artifactKind);
}
