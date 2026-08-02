using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "PortableProof")]
public sealed class PortableHandoffCliTests
{
    [Fact]
    public async Task HelpListsPortableHandoffCommands()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(["--help"], stdout, stderr, CancellationToken.None);
        var output = stdout.ToString();

        Assert.Equal(0, exitCode);
        Assert.Contains("validate-handoff", output, StringComparison.Ordinal);
        Assert.Contains("inspect-handoff", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateHandoffCommandSucceedsOnCopiedPackage()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable CLI Validate");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(
            ["validate-handoff", "--handoff-root", fixture.PortableRoot, "--schema-root", fixture.SchemaRoot],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("Readiness passed: True", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("First blocking finding: (none)", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InspectHandoffCommandDoesNotNeedSourceProjectFiles()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable CLI Inspect");
        fixture.DeleteSourceProject();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(
            ["inspect-handoff", "--handoff-root", fixture.PortableRoot, "--schema-root", fixture.SchemaRoot],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("Package hash:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("First blocking finding: (none)", stdout.ToString(), StringComparison.Ordinal);
    }
}
