namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Xml.Linq;

using Xunit;

public sealed class StorefrontComponentModeDependencyTests
{
    private static readonly string[] ModeProjectPaths =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
    ];

    private static string RetiredHybridProjectDirectory => "BlazorShop.PresentationV2/BlazorShop.Storefront.Components." + "Hybrid";

    private static string RetiredHybridProjectName => "BlazorShop.Storefront.Components." + "Hybrid";

    private static readonly string[] ForbiddenModeProjectReferenceFragments =
    [
        "BlazorShop.Storefront.Runtime",
        "BlazorShop.Storefront.Client",
        "BlazorShop.Storefront.V2",
        "BlazorShop.Storefront.V2.WASM",
        "BlazorShop.Storefront.Starter",
        "BlazorShop.Storefront.Starter.WASM",
        "BlazorShop.CommerceNode.API",
        "BlazorShop.ControlPlane",
        "BlazorShop.Application",
        "BlazorShop.Domain",
        "BlazorShop.Infrastructure",
        "BlazorShop.Web.SharedV2",
    ];

    private static readonly string[] ForbiddenReusableBrowserReferenceFragments =
    [
        "BlazorShop.Storefront.Browser",
    ];

    private static readonly string[] ForbiddenWasmHostReferenceFragments =
    [
        "BlazorShop.Storefront.Presentation",
        .. ForbiddenModeProjectReferenceFragments,
    ];

    private static readonly string[] ForbiddenV2WasmReferenceFragments =
    [
        "BlazorShop.Storefront.Runtime",
        "BlazorShop.Storefront.Client",
        "BlazorShop.Storefront.Starter",
        "BlazorShop.CommerceNode.API",
        "BlazorShop.ControlPlane",
        "BlazorShop.Application",
        "BlazorShop.Domain",
        "BlazorShop.Infrastructure",
        "BlazorShop.Web.SharedV2",
    ];

    [Fact]
    public void SsrReferencesExactlyComponentsAndPresentation()
    {
        var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj");

        Assert.Equal(
            [
                "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
            ],
            references);
    }

    [Fact]
    public void BaseComponentsReferencesNoProjectAndDoesNotReferenceBrowser()
    {
        var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");

        Assert.Empty(references);
        AssertNoReferencesContain(references, ForbiddenReusableBrowserReferenceFragments);
    }

    [Fact]
    public void RetiredHybridProjectIsAbsentFromActiveRepository()
    {
        var projectPath = $"{RetiredHybridProjectDirectory}/{RetiredHybridProjectName}.csproj";
        var solution = File.ReadAllText(RepositoryPath("BlazorShop.sln"));

        Assert.False(File.Exists(RepositoryPath(projectPath)));
        Assert.DoesNotContain(projectPath.Replace('/', '\\'), solution, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WasmHostReferencesExactlyComponentsAndBrowser()
    {
        var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj");

        Assert.Equal(
            [
                "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj",
                "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
            ],
            references);
        Assert.DoesNotContain(references, reference => reference.Contains("BlazorShop.Storefront.Presentation", StringComparison.Ordinal));
        AssertNoReferencesContain(references, ForbiddenWasmHostReferenceFragments);
    }

    [Fact]
    public void ModeProjectsDoNotReferenceRuntimeClientConsumersOrBackendProjects()
    {
        foreach (var projectPath in ModeProjectPaths)
        {
            var references = ReadProjectReferences(projectPath);

            foreach (var forbidden in ForbiddenModeProjectReferenceFragments)
            {
                Assert.DoesNotContain(references, reference => reference.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void V2WasmDoesNotReferenceRuntimeClientConsumersBackendCoreOrApiProjects()
    {
        var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");

        AssertNoReferencesContain(references, ForbiddenV2WasmReferenceFragments);
    }

    [Fact]
    public void V2DoesNotReferenceRetiredHybridProject()
    {
        var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");

        AssertNoReferencesContain(references, [RetiredHybridProjectName]);
    }

    [Fact]
    public void StorefrontPackagesHaveNoProjectReferenceCycles()
    {
        var projectPaths = Directory.EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2"), "BlazorShop.Storefront*.csproj", SearchOption.AllDirectories)
            .Select(NormalizeAbsolutePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var projectSet = projectPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var graph = projectPaths.ToDictionary(path => path, path => ResolveStorefrontProjectReferences(path, projectSet), StringComparer.OrdinalIgnoreCase);

        foreach (var project in projectPaths)
        {
            Assert.False(HasCycle(project, graph, [], []), $"Project-reference cycle detected from {Path.GetFileName(project)}.");
        }
    }

    private static bool HasCycle(
        string project,
        IReadOnlyDictionary<string, IReadOnlyList<string>> graph,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(project))
        {
            return false;
        }

        if (!visiting.Add(project))
        {
            return true;
        }

        foreach (var dependency in graph[project])
        {
            if (HasCycle(dependency, graph, visiting, visited))
            {
                return true;
            }
        }

        visiting.Remove(project);
        visited.Add(project);
        return false;
    }

    private static IReadOnlyList<string> ResolveStorefrontProjectReferences(string absoluteProjectPath, IReadOnlySet<string> storefrontProjectSet)
    {
        var directory = Path.GetDirectoryName(absoluteProjectPath) ?? RepositoryRoot;
        return XDocument.Load(absoluteProjectPath)
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeAbsolutePath(Path.GetFullPath(Path.Combine(directory, value))))
            .Where(storefrontProjectSet.Contains)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
    {
        return XDocument.Load(RepositoryPath(relativeProjectPath))
            .Descendants("ProjectReference")
            .Select(element => NormalizeRelativeReference(element.Attribute("Include")?.Value ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AssertNoReferencesContain(IReadOnlyList<string> references, IReadOnlyCollection<string> forbiddenFragments)
    {
        var offenders = references
            .Where(reference => forbiddenFragments.Any(fragment => reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(offenders.Length == 0, $"Forbidden project references:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static string NormalizeRelativeReference(string reference)
    {
        return reference.Replace('\\', '/');
    }

    private static string NormalizeAbsolutePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/');
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
