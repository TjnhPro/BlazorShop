namespace BlazorShop.Tests.Architecture
{
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed partial class StorefrontBuilderFoundationTests
    {
        private static readonly string[] RequiredSlotIds =
        [
            "layout.header",
            "layout.footer",
            "layout.main-navigation",
            "layout.mobile-navigation",
            "layout.cart-badge",
            "layout.account-menu",
            "home.sections",
            "catalog.product-card",
            "catalog.filters",
            "catalog.sorting",
            "catalog.pagination",
            "product.gallery",
            "product.information",
            "product.purchase",
            "cart.page",
            "checkout.page",
            "account.shell",
            "system.error",
        ];

        [Fact]
        public void StorefrontBuilderArchitectureNote_LocksDevelopmentTimeScope()
        {
            var note = ReadRepositoryFile("docs/visual-reverse-engineering-skill/StorefrontBuilder-architecture-note.md");

            Assert.Contains("development-time tooling", note, StringComparison.Ordinal);
            Assert.Contains("not a production ASP.NET service", note, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}", note, StringComparison.Ordinal);
            Assert.Contains("is a read-only template input", note, StringComparison.Ordinal);
            Assert.Contains("generated `BlazorShop.Storefront.{Name}` project is the editable", note, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterGenerationContract_DefinesSlotsProtectedZonesRoutesAndGates()
        {
            var contract = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml");

            foreach (var expected in new[]
            {
                "contractVersion:",
                "starterVersion:",
                "targetFramework: net10.0",
                "BlazorShop.Storefront.{Name}",
                "BlazorShop.Storefront.Client",
                "BlazorShop.Storefront.Runtime",
                "allowedGeneratedZones:",
                "managedZones:",
                "protectedZones:",
                "assetZones:",
                "analysisArtifactZone: docs/storefront-analysis",
                "featureManifest: Features/feature-manifest.json",
                "InitialSnapshot",
                "BrowserFetch",
                "RefreshAfterHydration",
                "cart.add-line",
                "run-storefront-starter-isolation-gate.ps1",
                "run-storefront-sample-release-gate.ps1",
            })
            {
                Assert.Contains(expected, contract, StringComparison.Ordinal);
            }

            foreach (var slotId in RequiredSlotIds)
            {
                Assert.Contains($"id: {slotId}", contract, StringComparison.Ordinal);
            }

            foreach (var protectedZone in new[]
            {
                "BlazorShop.Storefront.Client/Generated",
                "BlazorShop.Storefront.Runtime",
                "Endpoints/StarterBffEndpoints.cs",
                "Security/StarterReturnUrlValidator.cs",
                "StorefrontPackageVersions.props",
                "starter-generation.contract.yaml",
                "docs/storefront-analysis/generated-files.yaml",
            })
            {
                Assert.Contains(protectedZone, contract, StringComparison.Ordinal);
            }

            foreach (var renderOwner in new[] { "renderOwner: SSR", "renderOwner: Hybrid", "renderOwner: WASM-host" })
            {
                Assert.Contains(renderOwner, contract, StringComparison.Ordinal);
            }

            Assert.Contains("id: product.purchase", contract, StringComparison.Ordinal);
            Assert.Contains("action: cart.add-line", contract, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterGenerationContract_HasRequiredYamlShape()
        {
            var contract = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml");

            Assert.Matches(TopLevelKeyRegex(), contract);
            Assert.True(RequiredSlotIds.All(slot => contract.Contains($"id: {slot}", StringComparison.Ordinal)));
            Assert.True(CountOccurrences(contract, "route: ") >= 12);
            Assert.True(CountOccurrences(contract, "path: ") >= 12);
        }

        [Fact]
        public void StorefrontBuilderTooling_LivesOutsideProductionRuntimeProjects()
        {
            var requiredPaths = new[]
            {
                "tools/BlazorShop.AI.StorefrontBuilder/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/skills/storefront-builder/SKILL.md",
                "tools/BlazorShop.AI.StorefrontBuilder/knowledge/visual-reverse-engineering.md",
                "tools/BlazorShop.AI.StorefrontBuilder/knowledge/blazor-starter-boundaries.md",
                "tools/BlazorShop.AI.StorefrontBuilder/knowledge/ecommerce-visual-patterns.md",
                "tools/BlazorShop.AI.StorefrontBuilder/knowledge/asset-safety.md",
                "tools/BlazorShop.AI.StorefrontBuilder/schemas/storefront-builder-tooling.schema.json",
                "tools/BlazorShop.AI.StorefrontBuilder/templates/starter-input.template.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/scripts/capture/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/scripts/visual-qa/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/schemas/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/README.md",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/playwright/README.md",
            };

            foreach (var path in requiredPaths)
            {
                Assert.True(File.Exists(RepositoryPath(path)), $"Missing StorefrontBuilder tooling file '{path}'.");
            }

            var productionProjectReferences = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2"), "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(FindRepositoryRoot(), "*.sln", SearchOption.TopDirectoryOnly))
                .Select(path => new
                {
                    Path = path,
                    Content = File.ReadAllText(path),
                })
                .Where(file => file.Content.Contains("BlazorShop.AI.StorefrontBuilder", StringComparison.Ordinal)
                    || file.Content.Contains("tools/BlazorShop.AI.StorefrontBuilder", StringComparison.Ordinal)
                    || file.Content.Contains("tools\\BlazorShop.AI.StorefrontBuilder", StringComparison.Ordinal))
                .Select(file => ToRepositoryRelativePath(file.Path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(productionProjectReferences);
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
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

        [GeneratedRegex("(?m)^contractVersion:\\s*\\d+")]
        private static partial Regex TopLevelKeyRegex();
    }
}
