namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using System.Xml.Linq;

    using Xunit;

    public class ControlPlaneArchitectureBoundaryTests
    {
        [Fact]
        public void PresentationV2Projects_DoNotReferenceLegacyPresentationProjectsExceptWebShared()
        {
            var root = FindRepositoryRoot();
            var projects = new[]
            {
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.ControlPlane.API", "BlazorShop.ControlPlane.API.csproj"),
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.ControlPlane.Web", "BlazorShop.ControlPlane.Web.csproj")
            };

            var invalidReferences = projects
                .SelectMany(project => ReadProjectReferences(project)
                    .Where(reference => reference.Contains("BlazorShop.Presentation", StringComparison.OrdinalIgnoreCase)
                                        && !reference.EndsWith("BlazorShop.Web.Shared.csproj", StringComparison.OrdinalIgnoreCase))
                    .Select(reference => $"{project.Name} -> {reference}"))
                .ToArray();

            Assert.Empty(invalidReferences);
        }

        [Fact]
        public void ControlPlaneWeb_UsesOnlyAllowedWebSharedNamespaces()
        {
            var root = FindRepositoryRoot();
            var webRoot = root.CombinePath("BlazorShop.PresentationV2", "BlazorShop.ControlPlane.Web");
            var allowedPrefixes = new[]
            {
                "BlazorShop.Web.Shared",
                "BlazorShop.Web.Shared.BrowserStorage",
                "BlazorShop.Web.Shared.BrowserStorage.Contracts",
                "BlazorShop.Web.Shared.CookieStorage",
                "BlazorShop.Web.Shared.CookieStorage.Contracts",
                "BlazorShop.Web.Shared.Helper",
                "BlazorShop.Web.Shared.Helper.Contracts",
                "BlazorShop.Web.Shared.Services",
                "BlazorShop.Web.Shared.Services.Contracts"
            };

            var invalidUsings = Directory.EnumerateFiles(webRoot, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                               || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, index) => new { Path = path, Line = line.Trim(), Number = index + 1 }))
                .Where(item => item.Line.StartsWith("using BlazorShop.Web.", StringComparison.Ordinal)
                               || item.Line.StartsWith("@using BlazorShop.Web.", StringComparison.Ordinal))
                .Where(item => !allowedPrefixes.Any(prefix => item.Line.Contains(prefix, StringComparison.Ordinal)))
                .Select(item => $"{Path.GetRelativePath(root.FullName, item.Path)}:{item.Number}: {item.Line}")
                .ToArray();

            Assert.Empty(invalidUsings);
        }

        [Fact]
        public void ControlPlaneTests_DoNotReferenceLegacyPresentationUi()
        {
            var root = FindRepositoryRoot();
            var testRoots = new[]
            {
                root.CombinePath("BlazorShop.Tests", "Infrastructure", "ControlPlane"),
                root.CombinePath("BlazorShop.Tests", "PresentationV2", "ControlPlane")
            };

            var invalidReferences = testRoots
                .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                .Where(path => !path.EndsWith(nameof(ControlPlaneArchitectureBoundaryTests) + ".cs", StringComparison.Ordinal))
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
                .Where(item => item.Line.Contains("BlazorShop.Presentation.", StringComparison.Ordinal)
                               || item.Line.Contains("BlazorShop.Web.Pages", StringComparison.Ordinal)
                               || item.Line.Contains("BlazorShop.Storefront", StringComparison.Ordinal))
                .Select(item => $"{Path.GetRelativePath(root.FullName, item.Path)}:{item.Number}: {item.Line.Trim()}")
                .ToArray();

            Assert.Empty(invalidReferences);
        }

        private static IReadOnlyList<string> ReadProjectReferences(FileInfo projectFile)
        {
            var document = XDocument.Load(projectFile.FullName);
            return document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
            {
                current = current.Parent;
            }

            Assert.NotNull(current);
            return current!;
        }
    }

    internal static class ControlPlaneArchitecturePathExtensions
    {
        public static FileInfo Combine(this DirectoryInfo directory, params string[] segments)
        {
            return new FileInfo(Path.Combine([directory.FullName, .. segments]));
        }

        public static string CombinePath(this DirectoryInfo directory, params string[] segments)
        {
            return Path.Combine([directory.FullName, .. segments]);
        }
    }
}
