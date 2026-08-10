namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Xml.Linq;

using Xunit;

public sealed class StorefrontPrimitiveDependencyTests
{
    private const string PrimitiveProjectPath =
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj";

    private const string PrimitiveProjectDirectory =
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives";

    private static readonly string[] ForbiddenProjectReferenceFragments =
    [
        "BlazorShop.Storefront.Presentation",
        "BlazorShop.Storefront.Browser",
        "BlazorShop.Storefront.Runtime",
        "BlazorShop.Storefront.Client",
        "BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.Storefront.Components.WasmHost",
        "BlazorShop.Storefront.V2",
        "BlazorShop.Storefront.V2.WASM",
        "BlazorShop.Storefront.Starter",
        "BlazorShop.Storefront.Starter.WASM",
        "BlazorShop.Application",
        "BlazorShop.Domain",
        "BlazorShop.Infrastructure",
        "BlazorShop.CommerceNode.API",
        "BlazorShop.ControlPlane",
        "BlazorShop.Web.SharedV2",
    ];

    private static readonly string[] ForbiddenSourceTokens =
    [
        "@rendermode",
        "InteractiveServer",
        "InteractiveAuto",
        "InteractiveWebAssembly",
        "HttpClient",
        "HttpContext",
        "IHttpContextAccessor",
        "IJSRuntime",
        "HubConnection",
        "ClientWebSocket",
        "api/storefront/stores",
        "api/commerce",
        "api/control-plane",
        "CommerceNode",
        "ControlPlane",
        "localhost:",
        "127.0.0.1",
        "BlazorShop.Storefront.Presentation",
        "BlazorShop.Storefront.Browser",
        "BlazorShop.Storefront.Runtime",
        "BlazorShop.Storefront.Client",
        "BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.Storefront.Components.WasmHost",
        "BlazorShop.Storefront.V2",
        "class=\"group relative",
    ];

    private static readonly string[] SourceExtensions =
    [
        ".cs",
        ".razor",
        ".cshtml",
        ".js",
        ".mjs",
        ".ts",
        ".json",
        ".yaml",
        ".yml",
        ".css",
        ".scss",
        ".sass",
        ".less",
        ".xml",
        ".csproj",
    ];

    [Fact]
    public void ComponentsPrimitivesReferencesExactlyBaseComponents()
    {
        var references = ReadProjectReferences(RepositoryPath(PrimitiveProjectPath));

        Assert.Equal(
            ["../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj"],
            references);
        AssertNoReferencesContain(references, ForbiddenProjectReferenceFragments);
    }

    [Fact]
    public void ComponentsPrimitivesSourceContainsNoRuntimeApiOrHostTokens()
    {
        var violations = FindForbiddenSourceTokens(RepositoryPath(PrimitiveProjectDirectory));

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj", "ProjectReference")]
    [InlineData("../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj", "ProjectReference")]
    [InlineData(null, "@rendermode InteractiveWebAssembly")]
    [InlineData(null, "@inject HttpClient Http")]
    [InlineData(null, "@inject IJSRuntime JS")]
    [InlineData(null, "<article class=\"group relative flex\"></article>")]
    public void PrimitiveBoundaryScannerRejectsForbiddenFixtures(
        string? forbiddenProjectReference,
        string forbiddenSource)
    {
        using var fixture = PrimitiveFixture.Create(forbiddenProjectReference, forbiddenSource);

        var references = ReadProjectReferences(fixture.ProjectPath);
        var projectReferenceViolations = references
            .Where(reference => ForbiddenProjectReferenceFragments.Any(fragment =>
                reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .Select(reference => $"ProjectReference:{reference}")
            .ToArray();
        var sourceViolations = FindForbiddenSourceTokens(fixture.Root);
        var violations = projectReferenceViolations.Concat(sourceViolations).ToArray();

        Assert.NotEmpty(violations);
    }

    private static IReadOnlyList<string> ReadProjectReferences(string projectPath)
    {
        return XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(element => (element.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> FindForbiddenSourceTokens(string absoluteDirectory)
    {
        return Directory
            .EnumerateFiles(absoluteDirectory, "*", SearchOption.AllDirectories)
            .Where(IsSourceFile)
            .Where(file => !IsIgnoredPath(file))
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(RepositoryRoot, file).Replace('\\', '/');
                return ForbiddenSourceTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{relativePath}: forbidden '{token}'");
            })
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AssertNoReferencesContain(
        IReadOnlyCollection<string> references,
        IReadOnlyCollection<string> forbiddenFragments)
    {
        var offenders = references
            .Where(reference => forbiddenFragments.Any(fragment =>
                reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(offenders.Length == 0, $"Forbidden primitive project references:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static bool IsSourceFile(string file)
    {
        return SourceExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredPath(string file)
    {
        return file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class PrimitiveFixture : IDisposable
    {
        private PrimitiveFixture(string root, string projectPath)
        {
            this.Root = root;
            this.ProjectPath = projectPath;
        }

        public string Root { get; }

        public string ProjectPath { get; }

        public static PrimitiveFixture Create(string? forbiddenProjectReference, string forbiddenSource)
        {
            var root = Path.Combine(Path.GetTempPath(), "storefront-primitives-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var projectReferences = new List<string>
            {
                "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
            };
            if (!string.IsNullOrWhiteSpace(forbiddenProjectReference))
            {
                projectReferences.Add(forbiddenProjectReference);
            }

            var projectPath = Path.Combine(root, "Fixture.Primitives.csproj");
            File.WriteAllText(projectPath, Project(projectReferences));
            File.WriteAllText(Path.Combine(root, "BadPrimitive.razor"), forbiddenSource);

            return new PrimitiveFixture(root, projectPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(this.Root))
            {
                Directory.Delete(this.Root, recursive: true);
            }
        }
    }

    private static string Project(IEnumerable<string> references)
    {
        var projectReferences = string.Join(
            Environment.NewLine,
            references.Select(reference => $"    <ProjectReference Include=\"{reference}\" />"));

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <ItemGroup>
            {{projectReferences}}
              </ItemGroup>
            </Project>
            """;
    }
}
