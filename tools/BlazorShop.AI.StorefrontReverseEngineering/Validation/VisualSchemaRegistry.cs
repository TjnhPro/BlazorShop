namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public sealed class VisualSchemaRegistry : IVisualSchemaRegistry
{
    private static readonly string[] StandardRequiredProperties =
    [
        "schemaVersion",
        "artifactKind",
        "artifactId",
        "createdUtc"
    ];

    private readonly Dictionary<string, VisualSchemaDefinition> schemas;

    public VisualSchemaRegistry()
    {
        schemas = new(StringComparer.Ordinal)
        {
            ["visual-project"] = Create("visual-project"),
            ["configuration"] = Create("configuration"),
            ["reference-site-profile"] = Create("reference-site-profile"),
            ["reconnaissance"] = Create("reconnaissance"),
            ["capture-plan"] = Create("capture-plan"),
            ["capture-manifest"] = Create("capture-manifest"),
            ["capture-quality-report"] = Create("capture-quality-report"),
            ["screenshot-evidence"] = Create("screenshot-evidence"),
            ["dom-evidence"] = Create("dom-evidence"),
            ["computed-style-evidence"] = Create("computed-style-evidence"),
            ["asset-inventory"] = Create("asset-inventory"),
            ["interaction-evidence"] = Create("interaction-evidence"),
            ["page-topology-draft"] = Create("page-topology-draft"),
            ["page-specification-draft"] = Create("page-specification-draft"),
            ["component-specification-draft"] = Create("component-specification-draft"),
            ["visual-blueprint-draft"] = Create("visual-blueprint-draft"),
            ["originality-audit"] = Create("originality-audit"),
            ["readiness-report"] = Create("readiness-report"),
            ["workflow-run"] = Create("workflow-run")
        };
    }

    public IReadOnlyCollection<VisualSchemaDefinition> Schemas => schemas.Values;

    public VisualSchemaDefinition GetRequired(string artifactKind)
    {
        if (schemas.TryGetValue(artifactKind, out var schema))
        {
            return schema;
        }

        throw new InvalidOperationException($"[SRE-SCHEMA-004] Unknown artifact kind. Problem: '{artifactKind}' is not registered. Cause: every first-class artifact must be registered before read/write. Fix: add the artifact kind to VisualSchemaRegistry.");
    }

    private static VisualSchemaDefinition Create(string artifactKind) =>
        new(artifactKind, "1.0", StandardRequiredProperties);
}
