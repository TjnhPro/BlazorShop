using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class CoreContractTests
{
    [Fact]
    public void VisualProjectId_NormalizesName()
    {
        Assert.Equal("demo-store", VisualProjectId.Create(" Demo Store! ").Value);
    }

    [Fact]
    public void ReferenceUrl_AcceptsHttpHttpsAndFixtureFileUrls()
    {
        Assert.Equal("https://example.test/", ReferenceUrl.Create("https://example.test").Value);
        Assert.Equal("http://example.test/", ReferenceUrl.Create("http://example.test").Value);
        Assert.StartsWith("file:///", ReferenceUrl.Create("file:///tmp/storefront.html").Value, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreContracts_RoundTripJson()
    {
        var project = new VisualProject(
            "1.0",
            "visual-project",
            "project-demo",
            DateTimeOffset.Parse("2026-01-02T03:04:05Z"),
            "demo",
            "Demo",
            "https://example.test/",
            "obj/storefront-reverse-engineering/projects/demo",
            VisualProjectStatus.Created);

        var json = JsonSerializer.Serialize(project, VisualJson.Options);
        var roundTrip = JsonSerializer.Deserialize<VisualProject>(json, VisualJson.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(project.ProjectId, roundTrip.ProjectId);
        Assert.Equal(VisualProjectStatus.Created, roundTrip.Status);
    }

    [Fact]
    public async Task CliHelp_ListsPlaceholderCommands()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliHost.RunAsync(["--help"], output, error, CancellationToken.None);
        var text = output.ToString();

        Assert.Equal(0, exitCode);
        Assert.Contains("init", text, StringComparison.Ordinal);
        Assert.Contains("discover", text, StringComparison.Ordinal);
        Assert.Contains("capture", text, StringComparison.Ordinal);
        Assert.Contains("inspect", text, StringComparison.Ordinal);
        Assert.Contains("validate", text, StringComparison.Ordinal);
    }
}
