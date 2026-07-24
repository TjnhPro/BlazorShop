namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;

    using Xunit;

    public sealed class StorefrontStarterFoundationBoundaryTests
    {
        private static readonly string[] StarterProjectPaths =
        [
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Sample/BlazorShop.Storefront.Sample.csproj",
        ];

        [Fact]
        public void StarterArchitectureRoles_AreDocumented()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-storefront-starter-foundation.md");
            var systemMap = ReadRepositoryFile("docs/architecture/01-system-map.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");
            var contractOwnership = ReadRepositoryFile("docs/architecture/10-v2-contract-ownership.md");

            Assert.Contains("Storefront.Starter` is the neutral skeleton source", adr, StringComparison.Ordinal);
            Assert.Contains("Storefront.Sample` is the first deterministic generated project", adr, StringComparison.Ordinal);
            Assert.Contains("Storefront.V2` remains the real storefront implementation and behavior reference", adr, StringComparison.Ordinal);
            Assert.Contains("manual `StorefrontApiClient` transport from Storefront V2", adr, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter", systemMap, StringComparison.Ordinal);
            Assert.Contains("Future `BlazorShop.Storefront.Starter`", folderGuide, StringComparison.Ordinal);
            Assert.Contains("Future `BlazorShop.Storefront.Sample`", folderGuide, StringComparison.Ordinal);
            Assert.Contains("StorefrontStarterFoundationBoundaryTests", contractOwnership, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterProtectedAreas_AreDocumented()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-storefront-starter-foundation.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");

            foreach (var expected in new[]
            {
                "generated client source",
                "runtime security primitives",
                "BFF transport/security code",
                "package/version manifests",
                "generated storefront manifests",
            })
            {
                Assert.Contains(expected, adr, StringComparison.Ordinal);
                Assert.Contains(expected, folderGuide, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StarterProjects_DoNotReferenceForbiddenProjects()
        {
            foreach (var relativeProjectPath in StarterProjectPaths)
            {
                if (!File.Exists(RepositoryPath(relativeProjectPath)))
                {
                    continue;
                }

                var references = ReadProjectReferences(relativeProjectPath);
                var offenders = references
                    .Where(IsForbiddenStarterProjectReference)
                    .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Assert.Empty(offenders);
            }
        }

        [Fact]
        public void StarterSource_DoesNotUseSharedBusinessModelsOrCopyManualTransport()
        {
            var sourceRoots = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Sample",
            };

            var violations = sourceRoots
                .Select(RepositoryPath)
                .Where(Directory.Exists)
                .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                        || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                })
                .Where(file => file.Source.Contains("BlazorShop.Web.SharedV2.Models", StringComparison.Ordinal)
                    || file.Source.Contains("StorefrontApiClient", StringComparison.Ordinal)
                    || file.Source.Contains("Generated/StorefrontClient.g.cs", StringComparison.Ordinal)
                    || file.Source.Contains("ProjectReference", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }

        [Fact]
        public void StarterProject_ConsumesStorefrontClientAsPackage()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");
            var versionProps = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StorefrontPackageVersions.props");
            var nugetConfig = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/nuget.config");
            var compatibility = ReadRepositoryFile("docs/storefront-platform/storefront-package-compatibility.md");
            var changelog = ReadRepositoryFile("docs/storefront-platform/storefront-client-changelog.md");

            Assert.Contains("<PackageReference Include=\"BlazorShop.Storefront.Client\" Version=\"$(StorefrontClientPackageVersion)\"", project, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProjectReference", project, StringComparison.Ordinal);
            Assert.Contains("<StorefrontClientPackageVersion>1.0.0-local</StorefrontClientPackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("local-storefront-packages", nugetConfig, StringComparison.Ordinal);
            Assert.Contains("| v1 | 1.x | compatible |", compatibility, StringComparison.Ordinal);
            Assert.Contains("1.0.0-local", changelog, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterProject_RestoresAndBuildsFromLocalStorefrontClientPackage()
        {
            var repositoryRoot = FindRepositoryRoot();
            var packageFeed = RepositoryPath("artifacts/storefront-packages");
            var starterProject = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");

            if (Directory.Exists(packageFeed))
            {
                Directory.Delete(packageFeed, recursive: true);
            }

            Directory.CreateDirectory(packageFeed);

            var packResult = RunProcess(
                "dotnet",
                [
                    "pack",
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj"),
                    "--no-restore",
                    "--output",
                    packageFeed,
                    "/p:PackageVersion=1.0.0-local",
                ],
                repositoryRoot);

            Assert.True(packResult.ExitCode == 0, FormatProcessFailure("Storefront client package did not pack.", packResult));

            var restoreResult = RunProcess("dotnet", ["restore", starterProject], repositoryRoot);
            Assert.True(restoreResult.ExitCode == 0, FormatProcessFailure("Starter did not restore from the local Storefront client package.", restoreResult));

            var buildResult = RunProcess("dotnet", ["build", starterProject, "--no-restore"], repositoryRoot);
            Assert.True(buildResult.ExitCode == 0, FormatProcessFailure("Starter did not build after package restore.", buildResult));
        }

        [Fact]
        public void StarterDocs_SayStorefrontV2IsBehaviorReferenceOnly()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-storefront-starter-foundation.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");

            Assert.Contains("behavior reference", adr, StringComparison.Ordinal);
            Assert.Contains("must not be copied into Starter", adr, StringComparison.Ordinal);
            Assert.Contains("Copy Storefront V2 source", folderGuide, StringComparison.Ordinal);
            Assert.Contains("Storefront V2 into a neutral template", folderGuide, StringComparison.Ordinal);
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

        private static bool IsForbiddenStarterProjectReference(string reference)
        {
            var normalized = reference.Replace('\\', '/');

            return normalized.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.Web/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase);
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

        private static string FormatProcessFailure(string message, ProcessResult result)
        {
            return string.Join(
                Environment.NewLine,
                message,
                $"Exit code: {result.ExitCode}",
                "stdout:",
                result.StandardOutput,
                "stderr:",
                result.StandardError);
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

        private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ProcessResult(process.ExitCode, standardOutput, standardError);
        }

        private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
    }
}
