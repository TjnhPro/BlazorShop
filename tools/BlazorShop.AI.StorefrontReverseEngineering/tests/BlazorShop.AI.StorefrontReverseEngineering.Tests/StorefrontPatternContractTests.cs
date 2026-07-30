using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class StorefrontPatternContractTests
{
    [Fact]
    public async Task StorefrontPattern_ValidStarterContractLoadsTypedArtifacts()
    {
        var projectRoot = await CreateProjectAsync("Phase3C Pattern Valid");

        var pattern = await new StorefrontPatternContractBuilder(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(pattern.PageContracts, page => page.PageId == "home");
        Assert.Contains(pattern.PageContracts, page => page.PageId == "product-detail");
        Assert.Contains(pattern.PageContracts, page => page.PageId == "checkout-shell");
        Assert.Contains(pattern.GenerationZones.GeneratedZones, zone => zone == "Components/Catalog");
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "storefront-pattern", "storefront-pattern.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "storefront-pattern", "page-contracts.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "storefront-pattern", "behavior-boundaries.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "storefront-pattern", "generation-zones.json")));
    }

    [Fact]
    public async Task StorefrontPattern_NonSlotIdLinesAreNotTreatedAsSlots()
    {
        var projectRoot = await CreateProjectAsync("Phase3C Pattern Scoped Slots");

        var pattern = await new StorefrontPatternContractBuilder(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.DoesNotContain(pattern.Slots, slot => slot.SlotId == "BlazorShop.Storefront.Presentation");
        Assert.DoesNotContain(pattern.Slots, slot => slot.SlotId == "product.selection-preview");
        Assert.Contains(pattern.Actions, action => action.ActionId == "product.selection-preview");
    }

    [Fact]
    public async Task StorefrontPattern_DuplicateSlotIdsFailValidation()
    {
        var projectRoot = await CreateProjectAsync("Phase3C Pattern Duplicate Slots");
        var contract = await CopyStarterContractAsync("duplicate-slots");
        await InsertListItemAsync(contract, "slots", [
            "  - id: product.purchase",
            "    owner: generated",
            "    path: Components/Catalog/DuplicatePurchase.razor"
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new StorefrontPatternContractBuilder(GetRepoRoot()).BuildAsync(projectRoot, contract, CancellationToken.None));

        Assert.Contains("duplicate slot ID: product.purchase", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StorefrontPattern_ProtectedGeneratedTargetFailsValidation()
    {
        var projectRoot = await CreateProjectAsync("Phase3C Pattern Protected Path");
        var contract = await CopyStarterContractAsync("protected-path");
        await InsertListItemAsync(contract, "slots", [
            "  - id: protected.collision",
            "    owner: generated",
            "    path: StorefrontPackageVersions.props"
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new StorefrontPatternContractBuilder(GetRepoRoot()).BuildAsync(projectRoot, contract, CancellationToken.None));

        Assert.Contains("protected path collision", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StorefrontPattern_SameOriginBffActionPolicyIsPreserved()
    {
        var projectRoot = await CreateProjectAsync("Phase3C Pattern Actions");

        var pattern = await new StorefrontPatternContractBuilder(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(pattern.BehaviorBoundaries, boundary => boundary.Policy == "same-origin-bff-only");
        Assert.All(pattern.Actions, action => Assert.True(action.SameOriginBffOnly));
        Assert.Contains(pattern.Actions, action => action.ActionId == "cart.add-line" && action.Descriptor == "data-storefront-command=\"cart.add-line\"");
    }

    [Fact]
    public async Task StorefrontPattern_DirectStorefrontApiBrowserRouteFailsValidation()
    {
        var projectRoot = await CreateProjectAsync("Phase3C Pattern Unsafe Action");
        var contract = await CopyStarterContractAsync("unsafe-action");
        await InsertListItemAsync(contract, "actionDescriptors", [
            "  - id: unsafe.direct-api",
            "    owner: BlazorShop.Storefront.Presentation",
            "    route: /api/storefront/stores/sample/cart"
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new StorefrontPatternContractBuilder(GetRepoRoot()).BuildAsync(projectRoot, contract, CancellationToken.None));

        Assert.Contains("unsafe browser action route", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<string> CreateProjectAsync(string name)
    {
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "phase3c-pattern-" + Guid.NewGuid().ToString("N"));
        var project = await new VisualProjectService(GetRepoRoot())
            .InitializeAsync("https://example.test", name, outputRoot, force: false, CancellationToken.None);
        return project.ArtifactRoot;
    }

    private static async Task<string> CopyStarterContractAsync(string scenario)
    {
        var repoRoot = GetRepoRoot();
        var source = Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Starter", "starter-generation.contract.yaml");
        var targetRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "contract-mutations", scenario + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetRoot);
        var target = Path.Combine(targetRoot, "starter-generation.contract.yaml");
        await File.WriteAllTextAsync(target, await File.ReadAllTextAsync(source));
        return target;
    }

    private static async Task InsertListItemAsync(string path, string section, string[] linesToInsert)
    {
        var lines = (await File.ReadAllLinesAsync(path)).ToList();
        var index = lines.FindIndex(line => string.Equals(line, section + ":", StringComparison.Ordinal));
        if (index < 0)
        {
            throw new InvalidOperationException("Section not found: " + section);
        }

        lines.InsertRange(index + 1, linesToInsert);
        await File.WriteAllLinesAsync(path, lines);
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
