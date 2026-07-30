using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;

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
        schemas = LoadSchemas().ToDictionary(schema => schema.ArtifactKind, StringComparer.Ordinal);
        if (schemas.TryGetValue("capture-viewport-manifest", out var viewportManifest) &&
            schemas.TryGetValue("page-capture-manifest", out var pageManifest))
        {
            schemas.TryAdd("capture-manifest", viewportManifest with
            {
                ArtifactKind = "capture-manifest",
                RequiredPaths = (viewportManifest.RequiredPaths ?? [])
                    .Concat(pageManifest.RequiredPaths ?? [])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            });
        }
    }

    public IReadOnlyCollection<VisualSchemaDefinition> Schemas => schemas.Values;

    public VisualSchemaDefinition GetRequired(string artifactKind)
    {
        if (schemas.TryGetValue(artifactKind, out var schema))
        {
            return schema;
        }

        throw new InvalidOperationException($"[SRE-SCHEMA-004] Unknown artifact kind. Problem: '{artifactKind}' is not registered. Cause: every first-class artifact must be registered before read/write. Fix: add a schema file under tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas.");
    }

    private static IReadOnlyList<VisualSchemaDefinition> LoadSchemas()
    {
        var schemaRoot = FindSchemaRoot();
        if (schemaRoot is null)
        {
            throw new InvalidOperationException("[SRE-SCHEMA-009] Schema directory was not found. Problem: Schemas/*.schema.json could not be located. Cause: the tool was run without source or copied schema files. Fix: run from the repository root or include schema files beside the executable.");
        }

        return Directory.EnumerateFiles(schemaRoot, "*.schema.json")
            .Select(path => JsonSerializer.Deserialize<VisualSchemaDefinition>(File.ReadAllText(path), VisualJson.Options)
                ?? throw new InvalidOperationException($"[SRE-SCHEMA-010] Schema file is invalid JSON. Problem: '{path}' did not deserialize. Cause: schema descriptor is malformed. Fix: regenerate the schema file."))
            .ToArray();
    }

    private static string? FindSchemaRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Schemas"),
            Path.Combine(Environment.CurrentDirectory, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"),
            Path.Combine(Environment.CurrentDirectory, "Schemas")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.schema.json").Any())
            {
                return candidate;
            }
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas");
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.schema.json").Any())
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public static VisualSchemaDefinition CreateFallback(string artifactKind) =>
        new(artifactKind, "1.0", StandardRequiredProperties);
}
