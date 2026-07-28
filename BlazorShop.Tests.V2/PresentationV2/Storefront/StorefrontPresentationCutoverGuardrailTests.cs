namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PresentationAddressClient = BlazorShop.Storefront.Services.Contracts.IStorefrontAddressClient;
using PresentationAuthClient = BlazorShop.Storefront.Services.Contracts.IStorefrontAuthClient;
using PresentationCartClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCartClient;
using PresentationCatalogClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCatalogClient;
using PresentationCheckoutClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCheckoutClient;
using PresentationContentClient = BlazorShop.Storefront.Services.Contracts.IStorefrontContentClient;
using PresentationCustomerClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCustomerClient;
using PresentationCurrentStoreProvider = BlazorShop.Storefront.Services.Contracts.IStorefrontCurrentStoreProvider;
using PresentationDisplayContextProvider = BlazorShop.Storefront.Services.Contracts.IStorefrontDisplayContextProvider;
using PresentationPaymentClient = BlazorShop.Storefront.Services.Contracts.IStorefrontPaymentClient;
using PresentationPriceFormatter = BlazorShop.Storefront.Services.Contracts.IStorefrontPriceFormatter;
using PresentationSessionResolver = BlazorShop.Storefront.Services.Contracts.IStorefrontSessionResolver;
using PresentationStoreConfigurationClient = BlazorShop.Storefront.Services.Contracts.IStorefrontStoreConfigurationClient;

public sealed class StorefrontPresentationCutoverGuardrailTests
{
    [Fact]
    public void StorefrontPresentation_DIGraph_IsHostIndependent()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddStorefrontRuntime(options =>
        {
            options.StoreKey = "sample";
            options.CommerceNodeBaseUrl = "https://commerce-node.example/";
        });
        services.AddStorefrontPlatformRuntime();
        services.AddStorefrontPresentation(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        AssertPresentationOwned(scoped.GetRequiredService<PresentationAddressClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationAuthClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCartClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCatalogClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCheckoutClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationContentClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCustomerClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCurrentStoreProvider>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationDisplayContextProvider>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationPaymentClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationPriceFormatter>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationSessionResolver>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationStoreConfigurationClient>());
    }

    [Fact]
    public void StorefrontVisualViews_DoNotOwnRoutesOrSeoHead()
    {
        var violations = FindVisualHeadOwners(
            [
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
            ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void StorefrontStarter_ViewsRenderPresentationContextsOnly()
    {
        var starterRoot = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter";
        var starterProject = ReadRepositoryFile($"{starterRoot}/BlazorShop.Storefront.Starter.csproj");
        var starterHome = ReadRepositoryFile($"{starterRoot}/Pages/Ssr/Home/HomePage.razor");

        Assert.DoesNotContain("StorefrontBootstrapService", starterProject, StringComparison.Ordinal);
        Assert.False(File.Exists(RepositoryPath($"{starterRoot}/Services/StorefrontBootstrapService.cs")));
        Assert.DoesNotContain("PackageReference Include=\"BlazorShop.Storefront.Client\"", starterProject, StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference Include=\"BlazorShop.Storefront.Runtime\"", starterProject, StringComparison.Ordinal);
        Assert.Contains("ProjectReference Include=\"..\\BlazorShop.Storefront.Presentation\\BlazorShop.Storefront.Presentation.csproj\"", starterProject, StringComparison.Ordinal);

        Assert.Contains("StorefrontHomePageContext", starterHome, StringComparison.Ordinal);
        Assert.Contains("Context.DisplayContext", starterHome, StringComparison.Ordinal);
        Assert.Contains("Context.FeatureCapabilities", starterHome, StringComparison.Ordinal);
        Assert.DoesNotContain("StarterStorefrontOptions", starterHome, StringComparison.Ordinal);
        Assert.DoesNotContain("BootstrapService", starterHome, StringComparison.Ordinal);

        var sourceViolations = FindStarterSourceViolations(starterRoot);
        Assert.Empty(sourceViolations);

        var viewViolations = FindStarterViewContextViolations($"{starterRoot}/Pages");
        Assert.Empty(viewViolations);
    }

    [Fact]
    public void StorefrontRoutes_ArePresentationAssemblyOnly()
    {
        var routes = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor");
        var presentationDi = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/DependencyInjection/StorefrontPresentationServiceCollectionExtensions.cs");
        var v2Registration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs");
        var starterRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs");

        Assert.Contains("AppAssembly=\"@typeof(StorefrontApp).Assembly\"", routes, StringComparison.Ordinal);
        Assert.DoesNotContain("AdditionalAssemblies", routes, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontPresentationRouteOptions", presentationDi, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontPresentationRoutes", presentationDi, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontPresentationRoutes", v2Registration, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontPresentationRoutes", starterRegistration, StringComparison.Ordinal);

        var routeOwners = FindRazorPageOwners(
            [
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
            ]);

        Assert.Empty(routeOwners);
    }

    private static void AssertPresentationOwned(object service)
    {
        Assert.DoesNotContain(
            "BlazorShop.Storefront.V2",
            service.GetType().Assembly.GetName().Name,
            StringComparison.Ordinal);
    }

    private static string[] FindRazorPageOwners(string[] relativeRoots)
    {
        return relativeRoots
            .Select(RepositoryPath)
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(path => File.ReadLines(path).Any(line => line.TrimStart().StartsWith("@page ", StringComparison.Ordinal)))
            .Select(ToRepositoryRelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] FindVisualHeadOwners(string[] relativeRoots)
    {
        var forbiddenTokens = new[]
        {
            "<PageTitle",
            "<HeadContent",
            "<StorefrontSeoHead",
            "StorefrontResponseHeaders",
        };

        return relativeRoots
            .Select(RepositoryPath)
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Select(path => new
            {
                RelativePath = ToRepositoryRelativePath(path),
                Source = File.ReadAllText(path),
            })
            .Where(file => forbiddenTokens.Any(token => file.Source.Contains(token, StringComparison.Ordinal)))
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] FindStarterSourceViolations(string relativeRoot)
    {
        var forbiddenTokens = new[]
        {
            "BlazorShop.Storefront.Client",
            "IStorefrontStoreClient",
            "IStorefrontConfigurationClient",
            "IStorefrontCatalogClient",
            "IStorefrontCartClient",
            "IStorefrontCheckoutClient",
            "IStorefrontPaymentClient",
            "IStorefrontRuntime",
            "OnInitializedAsync",
        };

        return Directory
            .EnumerateFiles(RepositoryPath(relativeRoot), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Select(path => new
            {
                RelativePath = ToRepositoryRelativePath(path),
                Source = File.ReadAllText(path),
            })
            .Where(file => forbiddenTokens.Any(token => file.Source.Contains(token, StringComparison.Ordinal)))
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] FindStarterViewContextViolations(string relativePagesRoot)
    {
        return Directory
            .EnumerateFiles(RepositoryPath(relativePagesRoot), "*.razor", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).StartsWith("_", StringComparison.Ordinal))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Select(path => new
            {
                RelativePath = ToRepositoryRelativePath(path),
                Source = File.ReadAllText(path),
            })
            .Where(file => !file.Source.Contains("[Parameter, EditorRequired]", StringComparison.Ordinal)
                || !file.Source.Contains(" Context { get; set; }", StringComparison.Ordinal)
                || file.Source.Contains("@page", StringComparison.Ordinal)
                || file.Source.Contains("@inject IStorefront", StringComparison.Ordinal)
                || file.Source.Contains("OnInitializedAsync", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
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
        return Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/');
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
