namespace BlazorShop.Tests.Architecture
{
    using System.Diagnostics;
    using System.Text.Json;
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

        [Fact]
        public void StorefrontBuilderArtifactSchemas_ArePresentAndHaveValidationFixtures()
        {
            var requiredSchemas = new[]
            {
                "metadata.schema.json",
                "page-inventory.schema.json",
                "page-topology.schema.json",
                "design-tokens.schema.json",
                "ui-patterns.schema.json",
                "behaviors.schema.json",
                "responsive.schema.json",
                "capability-decisions.schema.json",
                "composition-manifest.schema.json",
                "generation-plan.schema.json",
                "generated-files.schema.json",
                "asset-manifest.schema.json",
                "ai-inference-log.schema.json",
            };

            foreach (var schemaFile in requiredSchemas)
            {
                var path = RepositoryPath($"tools/BlazorShop.AI.StorefrontBuilder/schemas/{schemaFile}");
                Assert.True(File.Exists(path), $"Missing StorefrontBuilder schema '{schemaFile}'.");

                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                Assert.True(root.TryGetProperty("$schema", out _), $"{schemaFile} is missing $schema.");
                Assert.Equal("object", root.GetProperty("type").GetString());
                Assert.True(root.GetProperty("required").GetArrayLength() >= 3, $"{schemaFile} must declare required fields.");
            }

            AssertRequiredFixturePasses("metadata.schema.json", "valid/metadata.json");
            AssertRequiredFixturePasses("generation-plan.schema.json", "valid/generation-plan.json");
            AssertRequiredFixtureFails("metadata.schema.json", "invalid/metadata.missing-generated-project.json");
            AssertRequiredFixtureFails("generation-plan.schema.json", "invalid/generation-plan.missing-files.json");
        }

        [Fact]
        public void StorefrontBuilderSchemaValidationScript_ReportsActionableInvalidFixtures()
        {
            var script = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderSchemas.ps1");

            Assert.Contains("Missing required field", script, StringComparison.Ordinal);
            Assert.Contains("metadata.missing-generated-project.json", script, StringComparison.Ordinal);
            Assert.Contains("generation-plan.missing-files.json", script, StringComparison.Ordinal);
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

        private static void AssertRequiredFixturePasses(string schemaFile, string fixtureFile)
        {
            var missing = GetMissingRequiredFields(schemaFile, fixtureFile);
            Assert.Empty(missing);
        }

        private static void AssertRequiredFixtureFails(string schemaFile, string fixtureFile)
        {
            var missing = GetMissingRequiredFields(schemaFile, fixtureFile);
            Assert.NotEmpty(missing);
        }

        private static string[] GetMissingRequiredFields(string schemaFile, string fixtureFile)
        {
            using var schema = JsonDocument.Parse(ReadRepositoryFile($"tools/BlazorShop.AI.StorefrontBuilder/schemas/{schemaFile}"));
            using var fixture = JsonDocument.Parse(ReadRepositoryFile($"tools/BlazorShop.AI.StorefrontBuilder/tests/schemas/fixtures/{fixtureFile}"));

            var required = schema.RootElement.GetProperty("required")
                .EnumerateArray()
                .Select(element => element.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            return required
                .Where(field => !fixture.RootElement.TryGetProperty(field!, out _))
                .Select(field => field!)
                .OrderBy(field => field, StringComparer.Ordinal)
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
