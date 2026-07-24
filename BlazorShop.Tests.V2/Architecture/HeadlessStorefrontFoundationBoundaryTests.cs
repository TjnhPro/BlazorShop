namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;

    using Xunit;

    public sealed class HeadlessStorefrontFoundationBoundaryTests
    {
        [Fact]
        public void HeadlessStorefrontRoles_AreDocumented()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-headless-storefront-platform-foundation.md");
            var systemMap = ReadRepositoryFile("docs/architecture/01-system-map.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");

            Assert.Contains("CommerceNode.API` is the headless ecommerce backend and Storefront API platform", adr, StringComparison.Ordinal);
            Assert.Contains("Storefront.V2` is the first real storefront consumer", adr, StringComparison.Ordinal);
            Assert.Contains("Storefront.Starter` is a future neutral skeleton", adr, StringComparison.Ordinal);
            Assert.Contains("Storefront.{Name}` represents future independent generated storefronts", adr, StringComparison.Ordinal);
            Assert.Contains("Headless Storefront target flow", systemMap, StringComparison.Ordinal);
            Assert.Contains("### `BlazorShop.Storefront.Client`", folderGuide, StringComparison.Ordinal);
            Assert.Contains("Optional `BlazorShop.Storefront.Runtime`", folderGuide, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_IsNotDocumentedAsStarterSource()
        {
            var architectureFiles = Directory
                .EnumerateFiles(RepositoryPath("docs/architecture"), "*.md", SearchOption.AllDirectories)
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                })
                .ToArray();

            var violations = architectureFiles
                .Where(file => file.Source.Contains("copy Storefront V2", StringComparison.OrdinalIgnoreCase)
                    && !file.Source.Contains("must not be copied", StringComparison.OrdinalIgnoreCase)
                    && !file.Source.Contains("not copied from Storefront V2", StringComparison.OrdinalIgnoreCase)
                    && !file.Source.Contains("Do not:", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }

        [Fact]
        public void StarterReadinessDecision_IsRecordedWithoutBuildingStarter()
        {
            var completionReport = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/Headless Storefront Platform Foundation.completion.md");
            var plan = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/Headless Storefront Platform Foundation.todo.md");

            Assert.Contains("Decision: ready for separate Starter planning.", completionReport, StringComparison.Ordinal);
            Assert.Contains("Starter implementation remains out of scope for this foundation.", completionReport, StringComparison.Ordinal);
            Assert.Contains("Do not build `BlazorShop.Storefront.Starter` in this foundation.", plan, StringComparison.Ordinal);
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.Storefront.Starter")));
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter")));
        }

        [Fact]
        public void FutureStorefrontPlatformProjects_DoNotReferenceBackendOrStorefrontV2()
        {
            var optionalProjects = new[]
            {
                "BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj",
                "BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj",
            };

            foreach (var relativeProjectPath in optionalProjects)
            {
                var projectPath = RepositoryPath(relativeProjectPath);
                if (!File.Exists(projectPath))
                {
                    continue;
                }

                var references = ReadProjectReferences(relativeProjectPath);
                var offenders = references
                    .Where(IsForbiddenStorefrontPlatformReference)
                    .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Assert.Empty(offenders);
            }
        }

        [Fact]
        public void StorefrontV2_DoesNotReferenceBackendProjectsOrSharedBusinessModels()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");
            var forbiddenReferences = references
                .Where(IsForbiddenStorefrontRuntimeReference)
                .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var dockerfile = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile");
            var forbiddenDockerCopies = new[]
            {
                "COPY BlazorShop.Application/",
                "COPY BlazorShop.Domain/",
                "COPY BlazorShop.Infrastructure/",
                "COPY BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/",
                "COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/",
            }
                .Where(copy => dockerfile.Contains(copy, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var sourceFiles = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2"), "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                });
            var forbiddenSourceImports = sourceFiles
                .Where(file => file.Source.Contains("BlazorShop.Application.", StringComparison.Ordinal)
                    || file.Source.Contains("BlazorShop.Domain.", StringComparison.Ordinal)
                    || file.Source.Contains("BlazorShop.Infrastructure.", StringComparison.Ordinal)
                    || file.Source.Contains("BlazorShop.Web.SharedV2.Models", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(forbiddenReferences);
            Assert.Empty(forbiddenDockerCopies);
            Assert.Empty(forbiddenSourceImports);
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
                .ToList();
        }

        private static bool IsForbiddenStorefrontPlatformReference(string reference)
        {
            var normalized = reference.Replace('\\', '/');

            return normalized.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsForbiddenStorefrontRuntimeReference(string reference)
        {
            var normalized = reference.Replace('\\', '/');

            return normalized.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase);
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
            return Path.GetRelativePath(FindRepositoryRoot(), path)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}
