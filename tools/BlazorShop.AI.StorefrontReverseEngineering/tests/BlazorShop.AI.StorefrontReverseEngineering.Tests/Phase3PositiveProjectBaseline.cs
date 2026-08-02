namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

internal static class Phase3PositiveProjectBaseline
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string? _baselineRoot;

    public static async Task<string> CreateProjectCopyAsync(string purpose)
    {
        await EnsureBaselineAsync();

        var repoRoot = Phase3DNegativeReviewMutationTests.GetRepoRoot();
        var outputRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "phase3d-positive-copy-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(_baselineRoot!, outputRoot);
        Phase3TempPathRegistry.Register(outputRoot);
        return outputRoot;
    }

    private static async Task EnsureBaselineAsync()
    {
        if (_baselineRoot is not null)
        {
            return;
        }

        await Gate.WaitAsync();
        try
        {
            if (_baselineRoot is null)
            {
                _baselineRoot = await Phase3DPositiveEndToEndTests.CreateBaselineProjectAsync("Phase 3D Shared Baseline");
                Phase3TempPathRegistry.Register(_baselineRoot);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

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
