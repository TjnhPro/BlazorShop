namespace BlazorShop.AI.StorefrontReverseEngineering.Storage;

public interface IVisualArtifactStore
{
    string Root { get; }

    Task WriteJsonAsync<TArtifact>(
        ArtifactPath path,
        string artifactKind,
        TArtifact artifact,
        CancellationToken cancellationToken);

    Task<TArtifact> ReadJsonAsync<TArtifact>(
        ArtifactPath path,
        string artifactKind,
        CancellationToken cancellationToken);
}
