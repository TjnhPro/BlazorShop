using System.Text.Json.Nodes;

namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public interface IVisualSchemaValidator
{
    void Validate(string artifactKind, JsonNode artifact);
}
