namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontComponentMvpArchitectureTests
{
    [Fact]
    public void ComponentMvpRoute_IsPresentationOwnedHiddenAndNoindex()
    {
        var presentationRoute = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Ssr/System/ComponentMvpRoutePage.razor");

        Assert.Contains("@page \"/__qa/component-mvp\"", presentationRoute, StringComparison.Ordinal);
        Assert.Contains("StorefrontPageKind.ComponentMvp", presentationRoute, StringComparison.Ordinal);
        Assert.Contains("StorefrontComponentMvpPageContext", presentationRoute, StringComparison.Ordinal);
        Assert.Contains("ViewSet.ComponentMvpLab", presentationRoute, StringComparison.Ordinal);
        Assert.Contains("RobotsIndex: false", presentationRoute, StringComparison.Ordinal);
        Assert.Contains("RobotsFollow: false", presentationRoute, StringComparison.Ordinal);

        foreach (var routeFile in EnumerateRazorFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2")
            .Concat(EnumerateRazorFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM")))
        {
            var routeDirectives = ReadRepositoryFile(routeFile)
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("@page ", StringComparison.Ordinal))
                .ToArray();

            Assert.DoesNotContain(routeDirectives, directive => directive.Contains("/__qa/component-mvp", StringComparison.Ordinal));
            Assert.DoesNotContain(routeDirectives, directive => directive.Contains("/component-mvp", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ComponentMvpRoute_IsNotPublicNavigationOrSitemapSurface()
    {
        var publicSurfaceFiles = new[]
        {
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontRoutes.cs",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Seo/StorefrontSitemapService.cs",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontNavigationProvider.cs",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontPageNavigationProvider.cs",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontShellContextService.cs",
        };

        foreach (var publicSurfaceFile in publicSurfaceFiles)
        {
            var source = ReadRepositoryFile(publicSurfaceFile);

            Assert.DoesNotContain("/__qa/component-mvp", source, StringComparison.Ordinal);
            Assert.DoesNotContain("/__qa/", source, StringComparison.Ordinal);
        }

        foreach (var visualFile in EnumerateRazorFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2")
            .Where(file => !file.EndsWith("Components/System/StorefrontComponentMvpLab.razor", StringComparison.Ordinal)))
        {
            var source = ReadRepositoryFile(visualFile);

            Assert.DoesNotContain("/__qa/component-mvp", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ComponentMvpQaNamespace_IsExplicitMiddlewarePolicy()
    {
        var currentStoreMiddleware = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontCurrentStoreMiddleware.cs");
        var redirectMiddleware = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontPublicRedirectMiddleware.cs");

        Assert.Contains("IsArchitectureQaPath(path)", currentStoreMiddleware, StringComparison.Ordinal);
        Assert.Contains("string.Equals(path, \"/__qa\", StringComparison.OrdinalIgnoreCase)", currentStoreMiddleware, StringComparison.Ordinal);
        Assert.Contains("path.StartsWith(\"/__qa/\", StringComparison.OrdinalIgnoreCase)", currentStoreMiddleware, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/__qa\"", ReadExcludedPrefixesInitializer(currentStoreMiddleware), StringComparison.Ordinal);

        Assert.Contains("IsArchitectureQaPath(path)", redirectMiddleware, StringComparison.Ordinal);
        Assert.Contains("string.Equals(path, \"/__qa\", StringComparison.OrdinalIgnoreCase)", redirectMiddleware, StringComparison.Ordinal);
        Assert.Contains("path.StartsWith(\"/__qa/\", StringComparison.OrdinalIgnoreCase)", redirectMiddleware, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/__qa\"", ReadExcludedPrefixesInitializer(redirectMiddleware), StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentMvpViewSlot_IsOptionalAndUsesPresentationOwnedContext()
    {
        var viewSet = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs");
        var validator = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewOptionsValidator.cs");
        var v2Registration = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs");
        var context = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/System/StorefrontComponentMvpPageContext.cs");

        Assert.Contains("public Type? ComponentMvpLab { get; init; }", viewSet, StringComparison.Ordinal);
        Assert.Contains("GetOptionalSlots()", viewSet, StringComparison.Ordinal);
        Assert.Contains("nameof(this.ComponentMvpLab)", viewSet, StringComparison.Ordinal);
        Assert.Contains("[nameof(StorefrontFoundationViewSet.ComponentMvpLab)] = typeof(StorefrontComponentMvpPageContext)", validator, StringComparison.Ordinal);
        Assert.Contains("ComponentMvpLab = typeof(StorefrontComponentMvpLab)", v2Registration, StringComparison.Ordinal);
        Assert.Contains("StorefrontBrandLogoContext BrandLogo", context, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontCart", context, StringComparison.Ordinal);
        Assert.DoesNotContain("Order", context, StringComparison.Ordinal);
        Assert.DoesNotContain("Payment", context, StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentMvpLab_ComposesOnlyHostOwnedVisualsAndInteractiveWrappers()
    {
        var lab = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor");

        Assert.Contains("data-storefront-component-mvp", lab, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component-mvp-section=\"ssr\"", lab, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component-mvp-section=\"hybrid\"", lab, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component-mvp-section=\"wasmhost\"", lab, StringComparison.Ordinal);
        Assert.Contains("<StorefrontBrandLogo Context=\"Context.BrandLogo\" Classes=\"BrandLogoClasses\" />", lab, StringComparison.Ordinal);
        Assert.Contains("<StorefrontHybridRuntimeProbeSection @rendermode=\"InteractiveWebAssembly\" />", lab, StringComparison.Ordinal);
        Assert.Contains("<StorefrontDiscountedProductRailSection @rendermode=\"InteractiveWebAssembly\" />", lab, StringComparison.Ordinal);
        Assert.DoesNotContain("@page", lab, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", lab, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveAuto", lab, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", lab, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront/stores", lab, StringComparison.Ordinal);
    }

    [Fact]
    public void StorefrontV2Program_MapsV2WasmAndWasmHostAssembliesForComponentMvp()
    {
        var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

        Assert.Contains("app.MapStorefrontApplication(", program, StringComparison.Ordinal);
        Assert.Contains("typeof(V2FoundationViewRegistration)", program, StringComparison.Ordinal);
        Assert.Contains("typeof(BlazorShop.Storefront.V2.WASM.Components.Account.StorefrontAccountApp).Assembly", program, StringComparison.Ordinal);
        Assert.Contains("typeof(BlazorShop.Storefront.Components.WasmHost.Content.StorefrontContactFormApp).Assembly", program, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveAuto", program, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateRazorFiles(string relativeFolder)
    {
        var root = RepositoryRoot();
        var absoluteFolder = Path.Combine(root, relativeFolder.Replace('/', Path.DirectorySeparatorChar));

        return Directory.EnumerateFiles(absoluteFolder, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string ReadExcludedPrefixesInitializer(string source)
    {
        var start = source.IndexOf("ExcludedPrefixes", StringComparison.Ordinal);
        Assert.True(start >= 0, "ExcludedPrefixes initializer was not found.");

        var end = source.IndexOf("];", start, StringComparison.Ordinal);
        Assert.True(end > start, "ExcludedPrefixes initializer end was not found.");

        return source[start..end];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
