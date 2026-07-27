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
    private const string CutoverTodo = "SPF16 guardrail placeholder; enable after the matching cutover phase implements the final state.";

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

    [Fact(Skip = CutoverTodo)]
    public void StorefrontStarter_ViewsRenderPresentationContextsOnly()
    {
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
