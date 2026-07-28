namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontApplicationBootstrapTests
{
    [Fact]
    public void StorefrontApplicationBootstrap_IsOwnedByPresentationHosting()
    {
        var services = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationServiceCollectionExtensions.cs");
        var app = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs");

        Assert.Contains("AddStorefrontApplication", services, StringComparison.Ordinal);
        Assert.Contains("AddStorefrontRuntime", services, StringComparison.Ordinal);
        Assert.Contains("AddStorefrontPlatformRuntime", services, StringComparison.Ordinal);
        Assert.Contains("AddStorefrontPresentation(configuration)", services, StringComparison.Ordinal);
        Assert.Contains("AddAntiforgery", services, StringComparison.Ordinal);
        Assert.Contains("AddRateLimiter", services, StringComparison.Ordinal);
        Assert.Contains("AddRazorComponents", services, StringComparison.Ordinal);
        Assert.Contains("AddInteractiveWebAssemblyComponents", services, StringComparison.Ordinal);
        Assert.Contains("UseStorefrontApplication", app, StringComparison.Ordinal);
        Assert.Contains("UseForwardedHeaders", app, StringComparison.Ordinal);
        Assert.Contains("UseStaticFiles", app, StringComparison.Ordinal);
        Assert.Contains("UseMiddleware<StorefrontCurrentStoreMiddleware>", app, StringComparison.Ordinal);
        Assert.Contains("UseMiddleware<StorefrontPublicRedirectMiddleware>", app, StringComparison.Ordinal);
        Assert.Contains("UseRateLimiter", app, StringComparison.Ordinal);
        Assert.Contains("UseStorefrontPresentation", app, StringComparison.Ordinal);
        Assert.Contains("MapStorefrontApplication", app, StringComparison.Ordinal);
        Assert.Contains("MapStorefrontPresentation", app, StringComparison.Ordinal);
        Assert.Contains("MapRazorComponents<StorefrontApp>", app, StringComparison.Ordinal);
    }

    [Fact]
    public void StorefrontV2Program_UsesSharedApplicationBootstrap()
    {
        var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

        Assert.Contains("AddStorefrontApplication(builder.Configuration)", source, StringComparison.Ordinal);
        Assert.Contains("UseStorefrontApplication()", source, StringComparison.Ordinal);
        Assert.Contains("MapStorefrontApplication(", source, StringComparison.Ordinal);
        Assert.Contains("typeof(V2FoundationViewRegistration)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontV2Services", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UseStorefrontV2HostPipeline", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontRateLimitPolicies.ConfigureStorefrontRateLimiter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontApiEndpointResolver.ConfigureStorefrontHttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStorefrontPresentation", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StorefrontStarterProgram_UsesSharedApplicationBootstrap()
    {
        var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs");

        Assert.Contains("AddStorefrontApplication(builder.Configuration)", source, StringComparison.Ordinal);
        Assert.Contains("UseStorefrontApplication()", source, StringComparison.Ordinal);
        Assert.Contains("MapStorefrontApplication(typeof(StarterFoundationViewRegistration))", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontRuntime", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontPlatformRuntime", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStorefrontPresentation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddRazorComponents", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddAntiforgery", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UseStaticFiles", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStorefrontPresentation", source, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
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
