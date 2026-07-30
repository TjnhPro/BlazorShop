using System.Text.Json.Nodes;

namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public sealed class VisualSchemaValidator : IVisualSchemaValidator
{
    private readonly IVisualSchemaRegistry registry;

    public VisualSchemaValidator(IVisualSchemaRegistry registry)
    {
        this.registry = registry;
    }

    public void Validate(string artifactKind, JsonNode artifact)
    {
        var schema = registry.GetRequired(artifactKind);
        if (artifact is not JsonObject jsonObject)
        {
            throw new InvalidOperationException($"[SRE-SCHEMA-005] Artifact must be a JSON object. Problem: '{artifactKind}' was not an object. Cause: reverse-engineering artifacts require named metadata. Fix: write a JSON object artifact.");
        }

        foreach (var property in schema.RequiredProperties)
        {
            if (!jsonObject.ContainsKey(property) || jsonObject[property] is null)
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-006] Required artifact metadata is missing. Problem: '{artifactKind}' has no '{property}'. Cause: every JSON artifact needs provenance metadata. Fix: include '{property}' before writing the artifact.");
            }
        }

        var actualKind = jsonObject["artifactKind"]?.GetValue<string>();
        if (!string.Equals(actualKind, artifactKind, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"[SRE-SCHEMA-007] Artifact kind mismatch. Problem: expected '{artifactKind}' but found '{actualKind}'. Cause: artifact path and content disagree. Fix: write the artifact with the registered kind.");
        }

        var version = jsonObject["schemaVersion"]?.GetValue<string>();
        if (!string.Equals(version, schema.SchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"[SRE-SCHEMA-008] Unsupported schema version. Problem: '{artifactKind}' uses '{version}'. Cause: this tool validates schema version '{schema.SchemaVersion}'. Fix: migrate or regenerate the artifact.");
        }
    }
}
