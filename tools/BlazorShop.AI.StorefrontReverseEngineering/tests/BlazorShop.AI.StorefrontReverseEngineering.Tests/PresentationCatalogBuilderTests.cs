using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class PresentationCatalogBuilderTests
{
    [Fact]
    public async Task PresentationCatalog_ReadsFoundationStarterAndComponentSources()
    {
        var projectRoot = CreateProjectRoot();

        var catalog = await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(catalog.Components, entry => entry.ComponentId == "foundation.home-page");
        Assert.Contains(catalog.Components, entry => entry.ComponentId == "catalog.product-card");
        Assert.Contains(catalog.Components, entry => entry.ComponentId == "contract.product-summary-item");
        Assert.Contains(catalog.Components, entry => entry.ComponentId == "contract.storefront-cart-behavior" && entry.BehaviorOwnedByRuntime);
        Assert.Contains(catalog.SourcePaths, path => path.EndsWith("starter-generation.contract.yaml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PresentationCatalog_ValidationReportPassesForCurrentSources()
    {
        var projectRoot = CreateProjectRoot();

        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);
        var report = await ReadReportAsync(projectRoot);

        Assert.True(report.Passed);
        Assert.Empty(report.Findings);
    }

    private static string CreateProjectRoot()
    {
        var root = Path.Combine(GetRepoRoot(), "obj", "storefront-reverse-engineering", "projects", "presentation-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task<PresentationCatalogValidationReport> ReadReportAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "presentation-catalog", "catalog-validation-report.json"));
        return JsonSerializer.Deserialize<PresentationCatalogValidationReport>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Presentation catalog report did not deserialize.");
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
