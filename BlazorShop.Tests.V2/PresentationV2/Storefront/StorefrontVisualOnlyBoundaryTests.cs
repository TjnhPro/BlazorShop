namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontVisualOnlyBoundaryTests
{
    private static readonly string[] VisualFolders =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Layouts"
    ];

    private static readonly string[] VisualApplicationTokens =
    [
        "@inject IStorefront",
        "IStorefrontRuntime",
        "HttpClient",
        "IHttpClientFactory",
        "IConfiguration",
        "IOptions<",
        "HttpContext",
        "RequestDelegate",
        "StorefrontApiEndpointResolver",
        "StorefrontStoreKeyResolver",
        "GetRequiredService",
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete"
    ];

    private static readonly string[] VisualDataLoadingLifecycleTokens =
    [
        "OnInitializedAsync",
        "OnParametersSetAsync"
    ];

    private static readonly string[] ApplicationClassNameFragments =
    [
        "Middleware",
        "Provider",
        "Resolver",
        "Client",
        "PageService",
        "Policy"
    ];

    private static readonly string[] ApplicationLogicFolders =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Middleware",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Options",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Models",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints"
    ];

    [Fact]
    public void MvpBlocker_V2VisualFolders_MustNotInjectApplicationServicesOrFrameworkPlumbing()
    {
        var offenders = FindSourceTokenOffenders(VisualFolders, VisualApplicationTokens, [".razor", ".cs"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 visual folders must render supplied context and must not inject application services or framework plumbing.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_V2RegisteredVisualComponents_MustNotLoadApplicationDataInAsyncLifecycle()
    {
        var offenders = FindSourceTokenOffenders(VisualFolders, VisualDataLoadingLifecycleTokens, [".razor", ".cs"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 registered visual components must not load application data in async lifecycle methods.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_V2Source_MustNotOwnApplicationServiceNamedClasses()
    {
        var offenders = EnumerateSourceFiles(["BlazorShop.PresentationV2/BlazorShop.Storefront.V2"], [".cs"])
            .Where(file => ApplicationClassNameFragments.Any(fragment =>
                Path.GetFileNameWithoutExtension(file.AbsolutePath).Contains(fragment, StringComparison.Ordinal)))
            .Select(file => file.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 source must not keep application-service named classes once it is visual-only.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_V2Source_MustNotKeepApplicationLogicFolders()
    {
        var offenders = ApplicationLogicFolders
            .SelectMany(folder => EnumerateSourceFiles([folder], [".cs", ".razor"]))
            .Select(file => file.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 must not keep active application logic source folders after the visual-only cutover.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static string[] FindSourceTokenOffenders(
        IReadOnlyCollection<string> relativeFolders,
        IReadOnlyCollection<string> forbiddenTokens,
        IReadOnlyCollection<string> extensions)
    {
        return EnumerateSourceFiles(relativeFolders, extensions)
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file.AbsolutePath);
                return forbiddenTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{file.RelativePath}: {token}");
            })
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<SourceFile> EnumerateSourceFiles(
        IReadOnlyCollection<string> relativeFolders,
        IReadOnlyCollection<string> extensions)
    {
        foreach (var relativeFolder in relativeFolders)
        {
            var absoluteFolder = RepositoryPath(relativeFolder);
            if (!Directory.Exists(absoluteFolder))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(absoluteFolder, "*.*", SearchOption.AllDirectories))
            {
                if (!extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
                    || IsBuildOutput(path))
                {
                    continue;
                }

                yield return new SourceFile(path, NormalizePath(Path.GetRelativePath(RepositoryRoot(), path)));
            }
        }
    }

    private static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                && File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private sealed record SourceFile(string AbsolutePath, string RelativePath);
}
