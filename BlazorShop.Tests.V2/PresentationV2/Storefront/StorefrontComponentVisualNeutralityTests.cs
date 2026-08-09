namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontComponentVisualNeutralityTests
{
    private static readonly string[] ModeProjectDirectories =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
    ];

    private static readonly string[] VisualAssetExtensions =
    [
        ".css",
        ".scss",
        ".sass",
        ".less",
    ];

    private static readonly string[] ForbiddenLiteralClassTokens =
    [
        "class=\"rounded",
        "class=\"bg-",
        "class=\"text-",
        "class=\"shadow",
        "class=\"grid",
        "class=\"flex",
        "class=\"px-",
        "class=\"mx-",
        "class=\"sm:",
        "class=\"md:",
        "class=\"lg:",
        "class=\"xl:",
        "class=\"2xl:",
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
    public void ModeProjectsDoNotContainLiteralTailwindOrV2VisualTokens()
    {
        foreach (var file in EnumerateSourceFiles())
        {
            var source = File.ReadAllText(file);

            foreach (var token in ForbiddenLiteralClassTokens)
            {
                Assert.DoesNotContain(token, source, StringComparison.Ordinal);
            }

            foreach (var token in ForbiddenV2VisualTokens)
            {
                Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void ModeProjectsAllowDynamicClassAndDataStorefrontAttributesOnlyAsNeutralHooks()
    {
        var dynamicClass = "class=\"@CssClass\"";
        var dataStorefrontHook = "data-storefront-cart";

        Assert.Contains("class=\"@", dynamicClass, StringComparison.Ordinal);
        Assert.Contains("data-storefront-", dataStorefrontHook, StringComparison.Ordinal);
        Assert.DoesNotContain(dynamicClass, ForbiddenLiteralClassTokens);
        Assert.DoesNotContain(dataStorefrontHook, ForbiddenLiteralClassTokens);
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
}
