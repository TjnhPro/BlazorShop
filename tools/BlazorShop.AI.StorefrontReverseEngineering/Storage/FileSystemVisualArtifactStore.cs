using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Storage;

public sealed class FileSystemVisualArtifactStore : IVisualArtifactStore
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public FileSystemVisualArtifactStore(
        string root,
        ApprovedArtifactRootResolver resolver,
        IVisualSchemaValidator validator)
    {
        Root = resolver.ResolveRoot(root);
        this.resolver = resolver;
        this.validator = validator;
    }

    public string Root { get; }

    public async Task WriteJsonAsync<TArtifact>(
        ArtifactPath path,
        string artifactKind,
        TArtifact artifact,
        CancellationToken cancellationToken)
    {
        var fullPath = resolver.ResolveArtifactPath(Root, path);
        var node = JsonSerializer.SerializeToNode(artifact, VisualJson.Options)
            ?? throw new InvalidOperationException("[SRE-SCHEMA-001] Artifact serialization returned no JSON.");

        validator.Validate(artifactKind, node);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            node.ToJsonString(VisualJson.Options) + Environment.NewLine,
            cancellationToken);
    }

    public async Task<TArtifact> ReadJsonAsync<TArtifact>(
        ArtifactPath path,
        string artifactKind,
        CancellationToken cancellationToken)
    {
        var fullPath = resolver.ResolveArtifactPath(Root, path);
        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException($"[SRE-SCHEMA-002] Artifact is empty. Problem: '{path}' has no JSON content. Cause: the artifact may be corrupt. Fix: re-run the owning workflow step.");

        validator.Validate(artifactKind, node);

        return node.Deserialize<TArtifact>(VisualJson.Options)
            ?? throw new InvalidOperationException($"[SRE-SCHEMA-003] Artifact cannot deserialize as requested type. Problem: '{path}' did not match the expected contract. Cause: artifact schema and .NET contract drifted. Fix: update schema or regenerate the artifact.");
    }
}
