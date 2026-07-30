namespace BlazorShop.AI.StorefrontReverseEngineering.Storage;

public sealed record ArtifactPath(string Value)
{
    public static ArtifactPath Create(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (Path.IsPathRooted(relativePath) ||
            normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal) ||
            normalized.Contains('\0'))
        {
            throw new InvalidOperationException($"[SRE-PATH-001] Unsafe artifact path. Problem: '{relativePath}' can escape the project root. Cause: artifact paths must be relative child paths. Fix: use a relative path without '..' segments.");
        }

        return new ArtifactPath(normalized);
    }

    public override string ToString() => Value;
}
