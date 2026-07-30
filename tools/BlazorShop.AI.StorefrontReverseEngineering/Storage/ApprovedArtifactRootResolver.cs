namespace BlazorShop.AI.StorefrontReverseEngineering.Storage;

public sealed class ApprovedArtifactRootResolver
{
    private readonly string repoRoot;

    public ApprovedArtifactRootResolver(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
    }

    public string ResolveRoot(string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var fullPath = Path.GetFullPath(Path.IsPathRooted(outputRoot)
            ? outputRoot
            : Path.Combine(repoRoot, outputRoot));

        var manualRoot = Path.Combine(repoRoot, "artifacts", "storefront-reverse-engineering");
        var automationRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering");

        if (!IsUnderRoot(fullPath, manualRoot) && !IsUnderRoot(fullPath, automationRoot))
        {
            throw new InvalidOperationException($"[SRE-PATH-002] Output root is not approved. Problem: '{outputRoot}' resolves outside reverse-engineering roots. Cause: Phase 3A artifacts must stay under artifacts/storefront-reverse-engineering or obj/storefront-reverse-engineering. Fix: choose an approved output root.");
        }

        return fullPath;
    }

    public string ResolveArtifactPath(string root, ArtifactPath artifactPath)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, artifactPath.Value));
        if (!IsUnderRoot(fullPath, fullRoot))
        {
            throw new InvalidOperationException($"[SRE-PATH-003] Artifact path escaped the project root. Problem: '{artifactPath.Value}' resolved outside '{fullRoot}'. Cause: path traversal is blocked before write. Fix: use a project-relative artifact path.");
        }

        return fullPath;
    }

    public static bool IsUnderRoot(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
