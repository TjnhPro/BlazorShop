namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;

    using Xunit;

    public sealed class StorefrontPresentationFoundationBoundaryTests
    {
        [Fact]
        public void PresentationProject_ReferencesOnlyRuntimeAndComponents()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");

            Assert.Equal(
                [
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj",
                ],
                references);
        }

        [Fact]
        public void PresentationProject_DoesNotReferenceHostOrBackendProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");
            var forbidden = references
                .Where(reference =>
                    reference.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Storefront.V2.WASM/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Storefront.Starter/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.ServiceDefaults/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.Empty(forbidden);
        }

        [Fact]
        public void BrowserAndComponentProjects_DoNotDependOnPresentationRuntimeOrClient()
        {
            var wasmReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");
            var componentReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");

            Assert.DoesNotContain(wasmReferences, IsPresentationRuntimeOrClientReference);
            Assert.DoesNotContain(componentReferences, IsPresentationRuntimeOrClientReference);
        }

        [Fact]
        public void ComponentsProject_RemainsLogicOnlyWithoutRazorFiles()
        {
            var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var razorFiles = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(razorFiles);
        }

        [Fact]
        public void PresentationProject_IsInSolutionAndHasFoundationFolders()
        {
            var solution = ReadRepositoryFile("BlazorShop.sln");
            Assert.Contains(
                "BlazorShop.PresentationV2\\BlazorShop.Storefront.Presentation\\BlazorShop.Storefront.Presentation.csproj",
                solution,
                StringComparison.Ordinal);

            var expectedFolders = new[]
            {
                "App",
                "Routing",
                "Pages",
                "PagePatterns",
                "Services",
                "Seo",
                "Endpoints",
                "Security",
                "Hosting",
                "Views/Foundation",
                "DependencyInjection",
            };

            foreach (var expectedFolder in expectedFolders)
            {
                Assert.True(
                    Directory.Exists(RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/{expectedFolder}")),
                    $"{expectedFolder} must exist in the Presentation foundation project.");
            }
        }

        private static bool IsPresentationRuntimeOrClientReference(string reference)
        {
            return reference.Contains("/BlazorShop.Storefront.Presentation/", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("/BlazorShop.Storefront.Runtime/", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("/BlazorShop.Storefront.Client/", StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
        {
            var projectPath = RepositoryPath(relativeProjectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new DirectoryNotFoundException($"Could not resolve project directory for {relativeProjectPath}.");
            var document = XDocument.Load(projectPath);

            return document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
                .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToRepositoryRelativePath(string path)
        {
            return Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/');
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate BlazorShop.sln.");
        }
    }
}
