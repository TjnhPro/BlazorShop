namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

internal sealed record PortableHandoffTestFixture(string SourceProjectRoot, string PortableRoot, string SchemaRoot)
{
    public static async Task<PortableHandoffTestFixture> CreateAsync(string name)
    {
        var repoRoot = Phase3DNegativeReviewMutationTests.GetRepoRoot();
        var sourceProjectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync(name);
        var portableRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "portable-handoff", "root-" + Guid.NewGuid().ToString("N"));
        var schemaRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "portable-handoff", "schemas-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(Path.Combine(sourceProjectRoot, "analysis", "agent-handoff"), Path.Combine(portableRoot, "analysis", "agent-handoff"));
        CopyDirectory(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"), schemaRoot);
        return new PortableHandoffTestFixture(sourceProjectRoot, portableRoot, schemaRoot);
    }

    public void DeleteSourceProject()
    {
        if (Directory.Exists(SourceProjectRoot))
        {
            Directory.Delete(SourceProjectRoot, recursive: true);
        }
    }

    public string PortableManifestPath => Path.Combine(PortableRoot, "analysis", "agent-handoff", "manifest.json");

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file)), overwrite: true);
        }
    }
}
