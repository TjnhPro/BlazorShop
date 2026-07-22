namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Xunit;

    public class ControlPlaneArchitectureBoundaryTests
    {
        [Fact]
        public void PresentationV2Projects_DoNotReferenceLegacyPresentationProjects()
        {
            var root = FindRepositoryRoot();
            var projects = new[]
            {
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.ControlPlane.API", "BlazorShop.ControlPlane.API.csproj"),
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.ControlPlane.Web", "BlazorShop.ControlPlane.Web.csproj"),
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.CommerceNode.API", "BlazorShop.CommerceNode.API.csproj"),
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "BlazorShop.Storefront.V2.csproj"),
                root.Combine("BlazorShop.PresentationV2", "BlazorShop.Web.SharedV2", "BlazorShop.Web.SharedV2.csproj")
            };

            var invalidReferences = projects
                .SelectMany(project => ReadProjectReferences(project)
                    .Where(reference => reference.Contains("BlazorShop.Presentation\\", StringComparison.OrdinalIgnoreCase)
                                        || reference.Contains("BlazorShop.Presentation/", StringComparison.OrdinalIgnoreCase))
                    .Select(reference => $"{project.Name} -> {reference}"))
                .ToArray();

            Assert.Empty(invalidReferences);
        }

        [Fact]
        public void PresentationV2Source_DoesNotContainLegacyRuntimeReferences()
        {
            var root = FindRepositoryRoot();
            var sourceRoot = root.CombinePath("BlazorShop.PresentationV2");
            var forbiddenReferences = new[]
            {
                new ForbiddenReference(new Regex(@"BlazorShop\.Presentation[\\/]", RegexOptions.IgnoreCase), "legacy Presentation path"),
                new ForbiddenReference(new Regex(@"\bBlazorShop\.API\b"), "legacy API namespace"),
                new ForbiddenReference(new Regex(@"\bBlazorShop\.Web\.Shared(?!V2)\b"), "legacy Web.Shared namespace"),
                new ForbiddenReference(new Regex(@"\bBlazorShop\.Web\b(?!\.SharedV2)"), "legacy Web namespace"),
                new ForbiddenReference(new Regex("adminclient", RegexOptions.IgnoreCase), "legacy adminclient service discovery")
            };

            var invalidReferences = EnumerateBoundarySourceFiles(sourceRoot)
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
                .SelectMany(item => forbiddenReferences
                    .Where(reference => reference.Pattern.IsMatch(item.Line))
                    .Select(reference => $"{Path.GetRelativePath(root.FullName, item.Path)}:{item.Number}: {reference.Description}: {item.Line.Trim()}"))
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
                "BlazorShop.Web.SharedV2",
                "BlazorShop.Web.SharedV2.BrowserStorage",
                "BlazorShop.Web.SharedV2.BrowserStorage.Contracts",
                "BlazorShop.Web.SharedV2.CookieStorage",
                "BlazorShop.Web.SharedV2.CookieStorage.Contracts",
                "BlazorShop.Web.SharedV2.Helper",
                "BlazorShop.Web.SharedV2.Helper.Contracts",
                "BlazorShop.Web.SharedV2.Services",
                "BlazorShop.Web.SharedV2.Services.Contracts",
                "BlazorShop.Web.SharedV2.Authentication",
                "BlazorShop.Web.SharedV2.Models.Authentication"
            };

            var invalidUsings = FindInvalidWebSharedUsings(root, webRoot, allowedPrefixes);

            Assert.Empty(invalidUsings);
        }

        [Fact]
        public void StorefrontV2_UsesOnlySharedV2Namespaces()
        {
            var root = FindRepositoryRoot();
            var webRoot = root.CombinePath("BlazorShop.PresentationV2", "BlazorShop.Storefront.V2");
            var allowedPrefixes = new[]
            {
                "BlazorShop.Web.SharedV2",
                "BlazorShop.Web.SharedV2.Models",
                "BlazorShop.Web.SharedV2.Models.Category",
                "BlazorShop.Web.SharedV2.Models.Discovery",
                "BlazorShop.Web.SharedV2.Models.Payment",
                "BlazorShop.Web.SharedV2.Models.Product",
                "BlazorShop.Web.SharedV2.Models.Seo"
            };

            var invalidUsings = FindInvalidWebSharedUsings(root, webRoot, allowedPrefixes);

            Assert.Empty(invalidUsings);
        }

        [Fact]
        public void SharedV2_DoesNotCopyLegacyFeatureServices()
        {
            var root = FindRepositoryRoot();
            var sharedV2Root = root.CombinePath("BlazorShop.PresentationV2", "BlazorShop.Web.SharedV2");
            var forbiddenPaths = new[]
            {
                Path.Combine("Services", "AdminAuditService.cs"),
                Path.Combine("Services", "AdminInventoryService.cs"),
                Path.Combine("Services", "AdminOrderService.cs"),
                Path.Combine("Services", "AdminProductService.cs"),
                Path.Combine("Services", "AdminUserService.cs"),
                Path.Combine("Services", "CartService.cs"),
                Path.Combine("Services", "CategoryService.cs"),
                Path.Combine("Services", "NewsletterService.cs"),
                Path.Combine("Services", "PaymentMethodService.cs"),
                Path.Combine("Services", "ProductService.cs"),
                Path.Combine("Services", "SeoRedirectService.cs"),
                Path.Combine("Services", "SeoSettingsService.cs"),
                Path.Combine("Models", "Admin"),
                Path.Combine("Models", "Analytics"),
                Path.Combine("Models", "Newsletter"),
                Path.Combine("Models", "Notifications")
            };

            var copiedLegacyFeatures = forbiddenPaths
                .Select(path => Path.Combine(sharedV2Root, path))
                .Where(path => File.Exists(path) || Directory.Exists(path))
                .Select(path => Path.GetRelativePath(root.FullName, path))
                .ToArray();

            Assert.Empty(copiedLegacyFeatures);
        }

        [Fact]
        public void ControlPlaneTests_DoNotReferenceLegacyPresentationUi()
        {
            var root = FindRepositoryRoot();
            var testRoots = new[]
            {
                root.CombinePath("BlazorShop.Tests.V2", "Infrastructure", "ControlPlane"),
                root.CombinePath("BlazorShop.Tests.V2", "PresentationV2", "ControlPlane")
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

        private static IReadOnlyList<string> FindInvalidWebSharedUsings(DirectoryInfo root, string sourceRoot, IReadOnlyList<string> allowedPrefixes)
        {
            return Directory.EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                               || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, index) => new { Path = path, Line = line.Trim(), Number = index + 1 }))
                .Where(item => item.Line.StartsWith("using BlazorShop.Web.", StringComparison.Ordinal)
                               || item.Line.StartsWith("@using BlazorShop.Web.", StringComparison.Ordinal))
                .Where(item => !IsAllowedUsing(item.Line, allowedPrefixes))
                .Select(item => $"{Path.GetRelativePath(root.FullName, item.Path)}:{item.Number}: {item.Line}")
                .ToArray();
        }

        private static IEnumerable<string> EnumerateBoundarySourceFiles(string sourceRoot)
        {
            var excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                "node_modules"
            };
            var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs",
                ".razor",
                ".csproj",
                ".props",
                ".targets",
                ".json",
                ".js",
                ".css",
                ".html",
                ".config",
                ".yml",
                ".yaml",
                ".md"
            };

            return Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(segment => excludedDirectories.Contains(segment)))
                .Where(path => textExtensions.Contains(Path.GetExtension(path))
                               || string.Equals(Path.GetFileName(path), "Dockerfile", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAllowedUsing(string line, IReadOnlyList<string> allowedPrefixes)
        {
            return allowedPrefixes.Any(prefix =>
                line.Equals($"using {prefix};", StringComparison.Ordinal)
                || line.Equals($"@using {prefix}", StringComparison.Ordinal)
                || line.StartsWith($"using {prefix}.", StringComparison.Ordinal)
                || line.StartsWith($"@using {prefix}.", StringComparison.Ordinal));
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

    internal sealed record ForbiddenReference(Regex Pattern, string Description);

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
