namespace BlazorShop.Tests.Architecture
{
    using Xunit;

    public sealed class StorefrontBuilderQaRegenerationTests
    {
        [Fact]
        public void AssetPipeline_RecordsProvenanceAndReplacementPlaceholders()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/build-asset-manifest.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderAssets.ps1");

            foreach (var marker in new[]
            {
                "sourceUrl",
                "checksum",
                "contentType",
                "detectedUsage",
                "normalizedFilename",
                "duplicateOf",
                "allowedToCopy",
                "replacementNeeded",
                "replacementList",
                "makes no production licensing claim",
            })
            {
                Assert.Contains(marker, generator, StringComparison.Ordinal);
                Assert.Contains(marker, validator, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-ASSET-003", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void StaticValidationGate_CoversArtifactsGuardsRoutesAssetsAndPackages()
        {
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderStaticGate.ps1");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/bad-static-project/Pages/Duplicate.razor");

            foreach (var marker in new[]
            {
                "Test-StorefrontBuilderSchemas.ps1",
                "Test-StorefrontBuilderGeneratedProject.ps1",
                "Test-StorefrontBuilderAssets.ps1",
                "Test-StorefrontBuilderGuard.ps1",
                "generated-files.yaml",
                "Duplicate route",
                "Package compatibility metadata",
                "PackageReference",
            })
            {
                Assert.Contains(marker, validator, StringComparison.Ordinal);
            }

            Assert.Contains("validate-storefront", command, StringComparison.Ordinal);
            Assert.Contains("@page \"/duplicate\"", fixture, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-001", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-002", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-003", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-004", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void BuildIsolationGate_RestoresBuildsPacksAndRejectsForbiddenReferences()
        {
            var gate = ReadRepositoryFile("scripts/qa/run-storefront-builder-isolation-gate.ps1");

            foreach (var marker in new[]
            {
                "dotnet restore",
                "dotnet build",
                "dotnet pack",
                "BlazorShop.Storefront.Client",
                "BlazorShop.Storefront.Runtime",
                "PackageReference",
                "BlazorShop.Storefront.V2",
                "BlazorShop.Application",
                "BlazorShop.Domain",
                "BlazorShop.Infrastructure",
                "BlazorShop.CommerceNode.API",
                "BlazorShop.ControlPlane.API",
                "StorefrontClientPackageVersion",
                "StorefrontRuntimePackageVersion",
                "Describe",
            })
            {
                Assert.Contains(marker, gate, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-ISOLATION-001", gate, StringComparison.Ordinal);
            Assert.Contains("SFB-ISOLATION-002", gate, StringComparison.Ordinal);
            Assert.Contains("SFB-ISOLATION-003", gate, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
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
