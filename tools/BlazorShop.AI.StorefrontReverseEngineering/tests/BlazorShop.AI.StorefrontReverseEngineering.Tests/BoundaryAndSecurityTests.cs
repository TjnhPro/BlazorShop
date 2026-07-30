using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class BoundaryAndSecurityTests
{
    [Fact]
    public void Boundary_NoProductionProjectReferencesReverseEngineering()
    {
        var repoRoot = GetRepoRoot();
        var productionRoots = new[]
        {
            "BlazorShop.PresentationV2",
            "BlazorShop.Domain",
            "BlazorShop.Application",
            "BlazorShop.Infrastructure"
        };

        foreach (var root in productionRoots)
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(repoRoot, root), "*.*", SearchOption.AllDirectories)
                         .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("BlazorShop.AI.StorefrontReverseEngineering", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Boundary_ReverseEngineeringDoesNotReferenceRuntimeOrGeneratedRoots()
    {
        var repoRoot = GetRepoRoot();
        var toolRoot = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering");
        var forbidden = new[]
        {
            "BlazorShop.Storefront.V2",
            "BlazorShop.CommerceNode.API",
            "BlazorShop.ControlPlane.API",
            "BlazorShop.Domain",
            "BlazorShop.Application",
            "BlazorShop.Infrastructure",
            "artifacts/storefront-builder/generated"
        };

        foreach (var file in Directory.EnumerateFiles(toolRoot, "*.csproj", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            var text = File.ReadAllText(file);
            foreach (var forbiddenText in forbidden)
            {
                Assert.DoesNotContain($"ProjectReference Include=\"{forbiddenText}", text, StringComparison.Ordinal);
                Assert.DoesNotContain($"ProjectReference Include=\"..\\..\\{forbiddenText}", text, StringComparison.Ordinal);
            }
        }

        foreach (var file in Directory.EnumerateFiles(toolRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            var text = File.ReadAllText(file);
            foreach (var forbiddenText in forbidden.Where(value => value.StartsWith("BlazorShop.", StringComparison.Ordinal)))
            {
                Assert.DoesNotContain($"using {forbiddenText}", text, StringComparison.Ordinal);
                Assert.DoesNotContain($"{forbiddenText}.csproj", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Boundary_ActiveSourceHasNoUnsupportedBrowserAdapterMarkers()
    {
        var repoRoot = GetRepoRoot();
        var sourceRoots = new[]
        {
            "Application",
            "Browser",
            "Cli",
            "Contracts",
            "Evidence",
            "Interactions",
            "Storage",
            "Validation",
            "Workflows"
        };
        var toolRoot = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering");

        foreach (var sourceRoot in sourceRoots)
        {
            var fullRoot = Path.Combine(toolRoot, sourceRoot);
            foreach (var file in Directory.EnumerateFiles(fullRoot, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("Not" + "Supported" + "Exception", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Security_RedactsCookiesTokensAndAuthorizationHeaders()
    {
        var text = "Authorization: Bearer secret Cookie: session=abc access_token=token123&password=hunter2 api_key=key";
        var redacted = SensitiveValueRedactor.Redact(text);

        Assert.DoesNotContain("secret", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("session=abc", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("token123", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("api_key=key", redacted, StringComparison.Ordinal);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
