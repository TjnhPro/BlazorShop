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

        foreach (var path in schema.RequiredPaths ?? [])
        {
            if (ResolvePath(jsonObject, path) is null)
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-011] Required artifact domain field is missing. Problem: '{artifactKind}' has no '{path}'. Cause: artifact-specific schema validation requires domain fields, not metadata only. Fix: regenerate the artifact with '{path}'.");
            }
        }

        foreach (var path in schema.ArrayPaths ?? [])
        {
            if (ResolvePath(jsonObject, path) is not JsonArray)
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-012] Artifact field must be an array. Problem: '{artifactKind}.{path}' is not an array. Cause: nested artifact shape is invalid. Fix: regenerate the artifact with an array at '{path}'.");
            }
        }

        foreach (var rule in schema.EnumRules ?? [])
        {
            if (ResolvePath(jsonObject, rule.Path) is not JsonValue value ||
                !rule.Values.Contains(value.GetValue<string>(), StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-013] Artifact enum value is invalid. Problem: '{artifactKind}.{rule.Path}' is outside the allowed set. Cause: artifact schema and content disagree. Fix: regenerate the artifact using one of: {string.Join(", ", rule.Values)}.");
            }
        }

        foreach (var rule in schema.NumericRules ?? [])
        {
            if (ResolvePath(jsonObject, rule.Path) is not JsonValue value ||
                !value.TryGetValue<decimal>(out var number) ||
                (rule.Minimum.HasValue && number < rule.Minimum.Value) ||
                (rule.Maximum.HasValue && number > rule.Maximum.Value))
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-014] Artifact numeric value is outside bounds. Problem: '{artifactKind}.{rule.Path}' violates the schema bounds. Cause: artifact has invalid numeric evidence. Fix: regenerate the artifact with a valid value.");
            }
        }

        foreach (var rule in schema.ArrayLengthRules ?? [])
        {
            if (ResolvePath(jsonObject, rule.Path) is not JsonArray array ||
                array.Count < rule.MinimumLength)
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-015] Artifact array length is below minimum. Problem: '{artifactKind}.{rule.Path}' requires at least {rule.MinimumLength} item(s). Cause: artifact evidence is empty or incomplete. Fix: regenerate the artifact with usable evidence.");
            }
        }

        foreach (var path in schema.NonEmptyStringPaths ?? [])
        {
            if (ResolvePath(jsonObject, path) is not JsonValue value ||
                !value.TryGetValue<string>(out var text) ||
                string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException($"[SRE-SCHEMA-016] Artifact string value is empty. Problem: '{artifactKind}.{path}' must be non-empty. Cause: artifact provenance or correlation is missing. Fix: regenerate the artifact with a non-empty '{path}'.");
            }
        }
    }

    private static JsonNode? ResolvePath(JsonObject root, string path)
    {
        JsonNode? current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is JsonObject jsonObject)
            {
                current = jsonObject[segment];
                continue;
            }

            return null;
        }

        return current;
    }
}
