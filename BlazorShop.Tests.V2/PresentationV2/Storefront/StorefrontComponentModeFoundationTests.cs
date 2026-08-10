namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Xml.Linq;

using Xunit;

public sealed class StorefrontComponentModeFoundationTests
{
    private static readonly (string Name, string Directory, string ProjectPath)[] ModeProjects =
    [
        (
            "BlazorShop.Storefront.Components.Ssr",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj"),
        (
            "BlazorShop.Storefront.Components.Hybrid",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj"),
        (
            "BlazorShop.Storefront.Components.WasmHost",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj"),
    ];

    private static readonly string[] NonAdoptingConsumersThatMustNotReferenceModeProjects =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj",
    ];

    [Fact]
    public void ModeProjectDirectoriesAndProjectFilesExist()
    {
        foreach (var project in ModeProjects)
        {
            Assert.True(Directory.Exists(RepositoryPath(project.Directory)), $"{project.Directory} must exist.");
            Assert.True(File.Exists(RepositoryPath(project.ProjectPath)), $"{project.ProjectPath} must exist.");
        }
    }

    [Fact]
    public void ModeProjectsUseRazorSdkNet10AndStorefrontPackageMetadata()
    {
        foreach (var project in ModeProjects)
        {
            var document = XDocument.Load(RepositoryPath(project.ProjectPath));
            var root = Assert.Single(document.Elements("Project"));
            var properties = document.Descendants("PropertyGroup")
                .Elements()
                .ToDictionary(element => element.Name.LocalName, element => element.Value);

            Assert.Equal("Microsoft.NET.Sdk.Razor", root.Attribute("Sdk")?.Value);
            Assert.Equal("net10.0", properties["TargetFramework"]);
            Assert.Equal(project.Name, properties["PackageId"]);
            Assert.Equal("1.0.0", properties["Version"]);
            Assert.Equal("BlazorShop", properties["Authors"]);
            Assert.Equal("https://github.com/TjnhPro/BlazorShop", properties["RepositoryUrl"]);
            Assert.False(string.IsNullOrWhiteSpace(properties["Description"]));
        }
    }

    [Fact]
    public void ModeProjectsAreIncludedInSolution()
    {
        var solution = ReadRepositoryFile("BlazorShop.sln");

        foreach (var project in ModeProjects)
        {
            Assert.Contains(project.ProjectPath.Replace('/', '\\'), solution, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BaseComponentsRemainHeadlessContractsOnly()
    {
        var project = XDocument.Load(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj"));
        var root = Assert.Single(project.Elements("Project"));
        var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");

        Assert.Equal("Microsoft.NET.Sdk", root.Attribute("Sdk")?.Value);
        Assert.Empty(Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories));
        Assert.False(Directory.Exists(Path.Combine(componentRoot, "Features")));
    }

    [Fact]
    public void NonAdoptingConsumersDoNotReferenceModeProjects()
    {
        foreach (var consumerProject in NonAdoptingConsumersThatMustNotReferenceModeProjects)
        {
            var source = ReadRepositoryFile(consumerProject);

            Assert.DoesNotContain("BlazorShop.Storefront.Components.Ssr", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components.Hybrid", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components.WasmHost", source, StringComparison.Ordinal);
        }
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(RepositoryPath(relativePath));
    }
}
