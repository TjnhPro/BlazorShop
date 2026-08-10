namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontRenderModeOwnershipTests
{
    private static readonly string[] ReusableComponentRoots =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
    ];

    private static readonly string[] PublicStorefrontRoots =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
    ];

    private static readonly string[] ApprovedInteractiveWebAssemblyOwners =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor",
    ];

    [Fact]
    public void ReusableComponentPackagesDoNotOwnRenderModeDirectives()
    {
        var violations = EnumerateSourceFiles(ReusableComponentRoots)
            .Where(file => File.ReadAllText(file.AbsolutePath).Contains("@rendermode", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void PublicStorefrontDoesNotUseServerOrAutoInteractivity()
    {
        var violations = FindForbiddenTokenViolations(
            EnumerateSourceFiles(PublicStorefrontRoots),
            ["InteractiveServer", "InteractiveAuto"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void InteractiveWebAssemblyPlacementIsOwnedOnlyByApprovedV2CompositionFiles()
    {
        var violations = FindInteractiveWebAssemblyOwnerViolations(
            EnumerateSourceFiles(PublicStorefrontRoots),
            ApprovedInteractiveWebAssemblyOwners);

        Assert.Empty(violations);
    }

    [Fact]
    public void RenderModeScannerRejectsReusableDirectiveServerModeAndUnapprovedWasmOwner()
    {
        var fixtureFiles = new[]
        {
            new SourceFile("Reusable/Bad.razor", "Reusable/Bad.razor", "@rendermode=\"InteractiveWebAssembly\""),
            new SourceFile("V2/BadServer.razor", "V2/BadServer.razor", "@rendermode=\"InteractiveServer\""),
            new SourceFile("V2/BadAuto.razor", "V2/BadAuto.razor", "@rendermode=\"InteractiveAuto\""),
        };

        var reusableRenderModeViolations = fixtureFiles
            .Where(file => file.Source.Contains("@rendermode", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .ToArray();
        var forbiddenTokenViolations = FindForbiddenTokenViolations(fixtureFiles, ["InteractiveServer", "InteractiveAuto"]);
        var unapprovedOwnerViolations = FindInteractiveWebAssemblyOwnerViolations(fixtureFiles, []);

        Assert.Contains("Reusable/Bad.razor", reusableRenderModeViolations);
        Assert.Contains(forbiddenTokenViolations, violation => violation.Contains("InteractiveServer", StringComparison.Ordinal));
        Assert.Contains(forbiddenTokenViolations, violation => violation.Contains("InteractiveAuto", StringComparison.Ordinal));
        Assert.Contains(unapprovedOwnerViolations, violation => violation.Contains("Reusable/Bad.razor", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> FindForbiddenTokenViolations(
        IEnumerable<SourceFile> files,
        IReadOnlyCollection<string> forbiddenTokens)
    {
        return files
            .SelectMany(file => forbiddenTokens
                .Where(token => file.Source.Contains(token, StringComparison.Ordinal))
                .Select(token => $"{file.RelativePath}: {token}"))
            .ToArray();
    }

    private static IReadOnlyList<string> FindInteractiveWebAssemblyOwnerViolations(
        IEnumerable<SourceFile> files,
        IReadOnlyCollection<string> approvedOwners)
    {
        var approvedOwnerSet = approvedOwners.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return files
            .Where(file => file.Source.Contains("@rendermode", StringComparison.Ordinal)
                || file.Source.Contains("InteractiveWebAssembly", StringComparison.Ordinal))
            .Where(file => !approvedOwnerSet.Contains(file.RelativePath))
            .Select(file => file.RelativePath)
            .ToArray();
    }

    private static IEnumerable<SourceFile> EnumerateSourceFiles(IReadOnlyCollection<string> roots)
    {
        return roots
            .Select(RepositoryPath)
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(IsSourceFile)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Select(file => new SourceFile(
                file,
                Path.GetRelativePath(RepositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllText(file)));
    }

    private static bool IsSourceFile(string file)
    {
        return Path.GetExtension(file) is ".cs" or ".razor";
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed record SourceFile(string AbsolutePath, string RelativePath, string Source);
}
