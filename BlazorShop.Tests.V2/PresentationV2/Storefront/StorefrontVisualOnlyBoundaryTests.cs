namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontVisualOnlyBoundaryTests
{
    private static string RetiredHybridProjectName => "BlazorShop.Storefront.Components." + "Hybrid";

    private static readonly string[] VisualFolders =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Layouts"
    ];

    private static readonly string[] VisualApplicationTokens =
    [
        "@inject IStorefront",
        "IStorefrontRuntime",
        "HttpClient",
        "IHttpClientFactory",
        "IConfiguration",
        "IOptions<",
        "HttpContext",
        "RequestDelegate",
        "StorefrontApiEndpointResolver",
        "StorefrontStoreKeyResolver",
        "GetRequiredService",
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete"
    ];

    private static readonly string[] VisualDataLoadingLifecycleTokens =
    [
        "OnInitializedAsync",
        "OnParametersSetAsync"
    ];

    private static readonly string[] ApplicationClassNameFragments =
    [
        "Middleware",
        "Provider",
        "Resolver",
        "Client",
        "PageService",
        "Policy"
    ];

    private static readonly string[] ApplicationLogicFolders =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Middleware",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Options",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Models",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints"
    ];

    private static readonly string[] ShellVisualComponentFiles =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontFooter.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontAccountMenu.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor"
    ];

    [Fact]
    public void MvpBlocker_V2VisualFolders_MustNotInjectApplicationServicesOrFrameworkPlumbing()
    {
        var offenders = FindSourceTokenOffenders(VisualFolders, VisualApplicationTokens, [".razor", ".cs"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 visual folders must render supplied context and must not inject application services or framework plumbing.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_V2RegisteredVisualComponents_MustNotLoadApplicationDataInAsyncLifecycle()
    {
        var offenders = FindSourceTokenOffenders(VisualFolders, VisualDataLoadingLifecycleTokens, [".razor", ".cs"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 registered visual components must not load application data in async lifecycle methods.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_V2Source_MustNotOwnApplicationServiceNamedClasses()
    {
        var offenders = EnumerateSourceFiles(["BlazorShop.PresentationV2/BlazorShop.Storefront.V2"], [".cs"])
            .Where(file => ApplicationClassNameFragments.Any(fragment =>
                Path.GetFileNameWithoutExtension(file.AbsolutePath).Contains(fragment, StringComparison.Ordinal)))
            .Select(file => file.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 source must not keep application-service named classes once it is visual-only.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_V2Source_MustNotKeepApplicationLogicFolders()
    {
        var offenders = ApplicationLogicFolders
            .SelectMany(folder => EnumerateSourceFiles([folder], [".cs", ".razor"]))
            .Select(file => file.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 must not keep active application logic source folders after the visual-only cutover.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void F1_33_V2ShellVisualComponents_RenderSuppliedContextOnly()
    {
        var forbiddenTokens = VisualApplicationTokens
            .Concat(VisualDataLoadingLifecycleTokens)
            .ToArray();
        var offenders = ShellVisualComponentFiles
            .Select(relativePath => new SourceFile(RepositoryPath(relativePath), relativePath))
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file.AbsolutePath);
                return forbiddenTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{file.RelativePath}: {token}");
            })
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"F1.33: registered shell visual components must render supplied context only.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void F1_40_V2Imports_DoNotExposeApplicationServiceOrTransportNamespaces()
    {
        var imports = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor"));
        var forbiddenImports = new[]
        {
            "@using global::System.Net.Http.Json",
            "@using BlazorShop.Storefront.Presentation.Models",
            "@using BlazorShop.Storefront.Presentation.Services",
            "@using BlazorShop.Storefront.Presentation.Contracts",
            "@using BlazorShop.Storefront.Runtime",
            "@using BlazorShop.Storefront.Client",
            "@using Microsoft.AspNetCore.Http",
        };

        foreach (var forbiddenImport in forbiddenImports)
        {
            var importLines = imports
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(line => line.Trim())
                .ToArray();

            Assert.DoesNotContain(forbiddenImport, importLines);
        }

        Assert.Contains("@using BlazorShop.Storefront.Presentation.Services.Catalog", imports, StringComparison.Ordinal);
        Assert.Contains("@using BlazorShop.Storefront.Presentation.Services.Product", imports, StringComparison.Ordinal);
        Assert.Contains("@using BlazorShop.Storefront.Components.Contracts.Catalog", imports, StringComparison.Ordinal);
    }

    [Fact]
    public void F1_41_V2ProjectReferences_StayVisualHostOnly()
    {
        var project = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"));
        var forbiddenReferences = new[]
        {
            "BlazorShop.Storefront.Runtime",
            "BlazorShop.Storefront.Client",
            "BlazorShop.CommerceNode.API",
            "BlazorShop.ControlPlane.API",
            "BlazorShop.ControlPlane.Web",
            "BlazorShop.Application",
            "BlazorShop.Domain",
            "BlazorShop.Infrastructure",
            "BlazorShop.Web.SharedV2",
        };

        foreach (var forbiddenReference in forbiddenReferences)
        {
            Assert.DoesNotContain(forbiddenReference, project, StringComparison.Ordinal);
        }

        Assert.Contains("BlazorShop.ServiceDefaults", project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components", project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components.Primitives", project, StringComparison.Ordinal);
        Assert.DoesNotContain(RetiredHybridProjectName, project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components.Ssr", project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Presentation", project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.V2.WASM", project, StringComparison.Ordinal);
        Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.Server", project, StringComparison.Ordinal);
    }

    [Fact]
    public void F1_41_ReferenceComponentModeReferences_AreNarrowAndAdoptedOnlyByV2()
    {
        var v2Project = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"));
        var wasmProject = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj"));
        var header = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor"));
        var contentPage = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor"));
        var home = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor"));
        var discountedRailSection = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Catalog/StorefrontDiscountedProductRailSection.razor"));

        Assert.Contains("BlazorShop.Storefront.Components.Ssr", v2Project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components.Primitives", v2Project, StringComparison.Ordinal);
        Assert.DoesNotContain(RetiredHybridProjectName, v2Project, StringComparison.Ordinal);
        Assert.DoesNotContain("BlazorShop.Storefront.Components.WasmHost.csproj", v2Project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components.WasmHost", wasmProject, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components.Primitives", wasmProject, StringComparison.Ordinal);

        Assert.Contains("<StorefrontBrandLogo", header, StringComparison.Ordinal);
        Assert.Contains("<StorefrontContactFormSection", contentPage, StringComparison.Ordinal);
        Assert.Contains("<StorefrontDiscountedProductRailSection", home, StringComparison.Ordinal);
        Assert.Contains("<StorefrontDiscountedProductRail", discountedRailSection, StringComparison.Ordinal);
        Assert.Contains("<StorefrontProductSummaryCard", discountedRailSection, StringComparison.Ordinal);

        AssertProjectsDoNotReferenceModeProjects([
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj",
        ]);

        var generatedProjects = new[]
            {
                "artifacts/storefront-builder",
                "obj/storefront-builder/generated",
            }
            .Select(RepositoryPath)
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(path)))
            .ToArray();

        foreach (var generatedProject in generatedProjects)
        {
            AssertProjectDoesNotReferenceModeProjects(generatedProject);
        }
    }

    [Fact]
    public void F1_41_V2ForbiddenApplicationFolders_HaveNoActiveSource()
    {
        var offenders = ApplicationLogicFolders
            .SelectMany(folder => EnumerateSourceFiles([folder], [".cs", ".razor", ".json"]))
            .Select(file => file.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"F1.41: Storefront V2 forbidden application folders must not contain active source.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void F1_41_V2NamespaceDeclarations_UseV2VisualOwnership()
    {
        var offenders = EnumerateSourceFiles(["BlazorShop.PresentationV2/BlazorShop.Storefront.V2"], [".cs", ".razor"])
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file.AbsolutePath);
                var declarations = source
                    .Split(Environment.NewLine, StringSplitOptions.None)
                    .Select((line, index) => new { Line = line.Trim(), LineNumber = index + 1 })
                    .Where(entry => entry.Line.StartsWith("namespace BlazorShop.Storefront", StringComparison.Ordinal)
                        || entry.Line.StartsWith("@namespace BlazorShop.Storefront", StringComparison.Ordinal))
                    .Where(entry => !entry.Line.StartsWith("namespace BlazorShop.Storefront.V2", StringComparison.Ordinal)
                        && !entry.Line.StartsWith("@namespace BlazorShop.Storefront.V2", StringComparison.Ordinal))
                    .Select(entry => $"{file.RelativePath}:{entry.LineNumber}: {entry.Line}");

                return declarations;
            })
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"F1.41: Storefront V2 namespace declarations must be visibly V2-owned, not shared Storefront application namespaces.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void F1_42_StarterPages_DoNotInjectApplicationServices()
    {
        var offenders = EnumerateSourceFiles(["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages"], [".razor"])
            .Where(file => !Path.GetFileName(file.AbsolutePath).StartsWith("_", StringComparison.Ordinal))
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file.AbsolutePath);
                return source.Contains("@inject", StringComparison.Ordinal)
                    ? new[] { $"{file.RelativePath}: @inject" }
                    : Array.Empty<string>();
            })
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"F1.42: Starter pages must render Presentation contexts and must not inject services.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void F1_42_StarterSource_DoesNotUseRuntimeOrClientDirectly()
    {
        var forbiddenTokens = new[]
        {
            "BlazorShop.Storefront.Runtime",
            "BlazorShop.Storefront.Client",
            "IStorefrontRuntime",
            "StorefrontApiClient",
            "HttpClient",
            "IHttpClientFactory",
        };
        var offenders = EnumerateSourceFiles(["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"], [".cs", ".razor"])
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file.AbsolutePath);
                return forbiddenTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{file.RelativePath}: {token}");
            })
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"F1.42: Starter source must not compile against Runtime, Client, or manual transport APIs.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void F1_42_StarterProgram_UsesSharedStorefrontApplicationBootstrap()
    {
        var program = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"));
        var forbiddenTokens = new[]
        {
            "AddStorefrontRuntime",
            "AddStorefrontPlatformRuntime",
            "AddStorefrontPresentation",
            "AddRazorComponents",
            "AddAntiforgery",
            "UseStaticFiles",
            "MapStorefrontPresentation",
            "MapRazorComponents",
            "StarterStorefrontOptions",
            "StarterFeatureActivationService",
            "StarterFeatureManifest.Load",
        };

        Assert.Contains("AddStorefrontApplication(builder.Configuration)", program, StringComparison.Ordinal);
        Assert.Contains("AddStarterFoundationViews()", program, StringComparison.Ordinal);
        Assert.Contains("UseStorefrontApplication()", program, StringComparison.Ordinal);
        Assert.Contains("MapStorefrontApplication(", program, StringComparison.Ordinal);
        Assert.Contains("typeof(StarterFoundationViewRegistration)", program, StringComparison.Ordinal);
        Assert.Contains("typeof(BlazorShop.Storefront.Starter.WASM.StarterWasmAssemblyMarker).Assembly", program, StringComparison.Ordinal);

        foreach (var forbiddenToken in forbiddenTokens)
        {
            Assert.DoesNotContain(forbiddenToken, program, StringComparison.Ordinal);
        }
    }

    private static string[] FindSourceTokenOffenders(
        IReadOnlyCollection<string> relativeFolders,
        IReadOnlyCollection<string> forbiddenTokens,
        IReadOnlyCollection<string> extensions)
    {
        return EnumerateSourceFiles(relativeFolders, extensions)
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file.AbsolutePath);
                return forbiddenTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{file.RelativePath}: {token}");
            })
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<SourceFile> EnumerateSourceFiles(
        IReadOnlyCollection<string> relativeFolders,
        IReadOnlyCollection<string> extensions)
    {
        foreach (var relativeFolder in relativeFolders)
        {
            var absoluteFolder = RepositoryPath(relativeFolder);
            if (!Directory.Exists(absoluteFolder))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(absoluteFolder, "*.*", SearchOption.AllDirectories))
            {
                if (!extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
                    || IsBuildOutput(path))
                {
                    continue;
                }

                yield return new SourceFile(path, NormalizePath(Path.GetRelativePath(RepositoryRoot(), path)));
            }
        }
    }

    private static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertProjectsDoNotReferenceModeProjects(IReadOnlyList<string> relativeProjectPaths)
    {
        foreach (var relativeProjectPath in relativeProjectPaths)
        {
            AssertProjectDoesNotReferenceModeProjects(RepositoryPath(relativeProjectPath));
        }
    }

    private static void AssertProjectDoesNotReferenceModeProjects(string projectPath)
    {
        var project = File.ReadAllText(projectPath);
        foreach (var forbiddenModeProject in new[]
        {
            "BlazorShop.Storefront.Components.Ssr",
            RetiredHybridProjectName,
            "BlazorShop.Storefront.Components.WasmHost",
        })
        {
            Assert.DoesNotContain(forbiddenModeProject, project, StringComparison.Ordinal);
        }
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                && File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private sealed record SourceFile(string AbsolutePath, string RelativePath);
}
