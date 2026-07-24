namespace BlazorShop.Tests.Architecture
{
    using Xunit;

    public sealed class StorefrontBuilderVisualGenerationTests
    {
        [Fact]
        public void DesignTokenExtraction_UsesComputedStylesAndCoversRequiredTokenGroups()
        {
            var script = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/extract-design-tokens.mjs");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/computed-styles.sample.json");

            Assert.Contains("computed-styles-first", script, StringComparison.Ordinal);
            Assert.Contains("screenshot", script, StringComparison.OrdinalIgnoreCase);
            foreach (var group in new[]
            {
                "colors",
                "semanticColors",
                "typographyFamilies",
                "fontSizes",
                "fontWeights",
                "lineHeights",
                "spacingScale",
                "containerWidths",
                "breakpoints",
                "borderWidths",
                "borderRadius",
                "shadows",
                "motionDurations",
                "motionEasing",
                "confidence",
                "evidenceIds",
                "inferenceIds",
            })
            {
                Assert.Contains(group, script, StringComparison.Ordinal);
            }

            Assert.Contains("fontFamily", fixture, StringComparison.Ordinal);
            Assert.Contains("backgroundColor", fixture, StringComparison.Ordinal);
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
