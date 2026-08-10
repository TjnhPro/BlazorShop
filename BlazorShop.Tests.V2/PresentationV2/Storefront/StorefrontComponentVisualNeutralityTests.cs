namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Text.RegularExpressions;
using Xunit;

public sealed class StorefrontComponentVisualNeutralityTests
{
    private static readonly string[] ModeProjectDirectories =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
    ];

    private static readonly string[] VisualAssetExtensions =
    [
        ".css",
        ".scss",
        ".sass",
        ".less",
    ];

    private static readonly string[] ForbiddenCopyStrings =
    [
        "Shop now",
        "Add to cart",
        "Checkout",
        "Sale",
        "Free shipping",
    ];

    private static readonly string[] ForbiddenV2VisualTokens =
    [
        "bs-storefront-",
        "storefront.css",
        "css/site.css",
        "css/wasm-site.css",
        "wwwroot/",
        "wwwroot\\",
        "/_content/BlazorShop.Storefront.V2",
    ];

    [Fact]
    public void ModeProjectsDoNotContainStylesheetsOrThemeAssets()
    {
        foreach (var directory in ModeProjectDirectories.Select(RepositoryPath))
        {
            Assert.DoesNotContain(
                Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories),
                file => VisualAssetExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
            Assert.Empty(Directory.EnumerateFiles(directory, "tailwind.config.*", SearchOption.AllDirectories));
            Assert.Empty(Directory.EnumerateFiles(directory, "postcss.config.*", SearchOption.AllDirectories));
            Assert.False(Directory.Exists(Path.Combine(directory, "wwwroot", "css")));
            Assert.False(Directory.Exists(Path.Combine(directory, "Theme")));
            Assert.False(Directory.Exists(Path.Combine(directory, "Themes")));
        }
    }

    [Fact]
    public void ModeProjectsDoNotContainLiteralClassesOrV2VisualTokens()
    {
        var literalClassViolations = StorefrontClassAttributeScanner.FindLiteralClassAttributesInModeProjects(
            RepositoryRoot,
            ModeProjectDirectories);

        AssertNoLiteralClassViolations(literalClassViolations);

        foreach (var file in EnumerateSourceFiles())
        {
            var source = File.ReadAllText(file);

            foreach (var token in ForbiddenV2VisualTokens)
            {
                Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("<div class=\"@CssClass\"></div>")]
    [InlineData("<div class=\"@Classes.Container\"></div>")]
    [InlineData("<div class=\"@GetCssClass()\"></div>")]
    [InlineData("<div class=\"@(BuildCssClass())\"></div>")]
    [InlineData("<section data-storefront-region=\"hero\"></section>")]
    [InlineData("<section><h2>Heading</h2></section>")]
    public void LiteralClassScannerAllowsDynamicClassesAndSemanticHooks(string markup)
    {
        AssertNoLiteralClassViolations(StorefrontClassAttributeScanner.FindLiteralClassAttributes(
            "Component.razor",
            markup));
    }

    [Theory]
    [InlineData("<div class=\"flex\"></div>", "flex")]
    [InlineData("<div class=\"p-6\"></div>", "p-6")]
    [InlineData("<div class=\"gap-4\"></div>", "gap-4")]
    [InlineData("<div class=\"items-center\"></div>", "items-center")]
    [InlineData("<div class=\"storefront-logo\"></div>", "storefront-logo")]
    [InlineData("<div class=\"rounded-xl bg-white\"></div>", "rounded-xl bg-white")]
    [InlineData("<div class=\"flex @CssClass\"></div>", "flex @CssClass")]
    [InlineData("<div class=\"@CssClass selected\"></div>", "@CssClass selected")]
    [InlineData("<div class=\"@(BuildCssClass()) selected\"></div>", "@(BuildCssClass()) selected")]
    public void LiteralClassScannerRejectsLiteralAndMixedClassValues(string markup, string expectedClassValue)
    {
        var violation = Assert.Single(StorefrontClassAttributeScanner.FindLiteralClassAttributes(
            "Component.razor",
            markup));

        Assert.Equal("Component.razor", violation.RelativePath);
        Assert.Equal(expectedClassValue, violation.AttributeValue);
        Assert.Contains("host projects own literal visual classes", violation.Remediation, StringComparison.Ordinal);
    }

    [Fact]
    public void ModeProjectsAllowDynamicClassAndDataStorefrontAttributesOnlyAsNeutralHooks()
    {
        var dynamicClass = "class=\"@CssClass\"";
        var dataStorefrontHook = "data-storefront-cart";

        Assert.Contains("class=\"@", dynamicClass, StringComparison.Ordinal);
        Assert.Contains("data-storefront-", dataStorefrontHook, StringComparison.Ordinal);
        Assert.Empty(StorefrontClassAttributeScanner.FindLiteralClassAttributes(
            "Component.razor",
            "<div class=\"@CssClass\" data-storefront-cart></div>"));
    }

    [Fact]
    public void ModeProjectsDoNotIntroduceFinalStorefrontCopyStrings()
    {
        foreach (var file in EnumerateSourceFiles())
        {
            var source = File.ReadAllText(file);

            foreach (var copy in ForbiddenCopyStrings)
            {
                Assert.DoesNotContain(copy, source, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles()
    {
        return ModeProjectDirectories
            .Select(RepositoryPath)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor" or ".cshtml" or ".js" or ".mjs" or ".ts" or ".json" or ".md" or ".xml" or ".csproj");
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void AssertNoLiteralClassViolations(IReadOnlyCollection<StorefrontLiteralClassViolation> violations)
    {
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private sealed record StorefrontLiteralClassViolation(
        string RelativePath,
        string AttributeValue,
        string Remediation)
    {
        public override string ToString()
        {
            return $"{RelativePath}: class=\"{AttributeValue}\". {Remediation}";
        }
    }

    private static class StorefrontClassAttributeScanner
    {
        private const string RemediationMessage =
            "Reusable mode projects must expose fully dynamic class slots or data-storefront-* semantic hooks; host projects own literal visual classes.";

        private static readonly Regex ClassAttributePattern = new(
            "(?<![\\w:-])class\\s*=\\s*(?:\"(?<double>[^\"]*)\"|'(?<single>[^']*)')",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DynamicClassValuePattern = new(
            "^(@[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*(?:\\([^\"'\\s<>]*\\))?|@\\([^\\r\\n<>]+\\))$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] MarkupExtensions =
        [
            ".razor",
            ".cshtml",
        ];

        private static readonly string[] IgnoredDirectoryNames =
        [
            "bin",
            "obj",
            ".regeneration-candidate",
            "artifacts",
            "generated",
            "tmp",
            "temp",
        ];

        public static IReadOnlyList<StorefrontLiteralClassViolation> FindLiteralClassAttributesInModeProjects(
            string repositoryRoot,
            IReadOnlyList<string> modeProjectDirectories)
        {
            return modeProjectDirectories
                .Select(relativeDirectory => Path.Combine(
                    repositoryRoot,
                    relativeDirectory.Replace('/', Path.DirectorySeparatorChar)))
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                .Where(IsMarkupFile)
                .Where(file => !IsIgnoredPath(file))
                .SelectMany(file => FindLiteralClassAttributes(
                    Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                    File.ReadAllText(file)))
                .ToArray();
        }

        public static IReadOnlyList<StorefrontLiteralClassViolation> FindLiteralClassAttributes(
            string relativePath,
            string markup)
        {
            return ClassAttributePattern
                .Matches(markup)
                .Select(match => match.Groups["double"].Success
                    ? match.Groups["double"].Value
                    : match.Groups["single"].Value)
                .Where(attributeValue => !IsAllowedClassValue(attributeValue))
                .Select(attributeValue => new StorefrontLiteralClassViolation(
                    relativePath.Replace(Path.DirectorySeparatorChar, '/'),
                    attributeValue,
                    RemediationMessage))
                .ToArray();
        }

        private static bool IsMarkupFile(string file)
        {
            return MarkupExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsIgnoredPath(string file)
        {
            return file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => IgnoredDirectoryNames.Contains(part, StringComparer.OrdinalIgnoreCase));
        }

        private static bool IsAllowedClassValue(string attributeValue)
        {
            var trimmedValue = attributeValue.Trim();
            return trimmedValue.Length == 0 || DynamicClassValuePattern.IsMatch(trimmedValue);
        }
    }
}
