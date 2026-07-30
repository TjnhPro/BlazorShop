using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class InteractionCaptureTests
{
    [Fact]
    public async Task Interaction_MobileMenu_ClickProducesEvidence()
    {
        var evidence = await CaptureAsync(new InteractionCapturePlan("mobile-menu-open", [new InteractionActionDefinition(InteractionActionType.ClickSelector, ".mobile-menu")]));

        Assert.Equal(InteractionModel.ClickDriven, evidence.InteractionModel);
        Assert.True(evidence.DomChanged);
        Assert.Empty(evidence.Errors);
    }

    [Fact]
    public async Task Interaction_Accordion_ClickProducesEvidence()
    {
        var evidence = await CaptureAsync(new InteractionCapturePlan("accordion-open", [new InteractionActionDefinition(InteractionActionType.ClickSelector, ".accordion")]));

        Assert.Equal(InteractionModel.ClickDriven, evidence.InteractionModel);
    }

    [Fact]
    public async Task Interaction_HoverProducesHoverModel()
    {
        var evidence = await CaptureAsync(new InteractionCapturePlan("product-card-hover", [new InteractionActionDefinition(InteractionActionType.HoverSelector, ".product-card")]));

        Assert.Equal(InteractionModel.HoverDriven, evidence.InteractionModel);
        Assert.True(evidence.StyleChanged);
    }

    [Fact]
    public async Task Interaction_MissingSelectorCanWarnOrBlock()
    {
        var warning = await CaptureAsync(new InteractionCapturePlan("missing-warning", [new InteractionActionDefinition(InteractionActionType.ClickSelector, ".does-not-exist")]));
        Assert.NotEmpty(warning.Warnings);
        Assert.Empty(warning.Errors);

        var blocking = await CaptureAsync(new InteractionCapturePlan("missing-blocking", [new InteractionActionDefinition(InteractionActionType.ClickSelector, ".does-not-exist")], MissingSelectorIsBlocking: true));
        Assert.NotEmpty(blocking.Errors);
    }

    [Fact]
    public async Task Interaction_UnsafeSelectorIsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CaptureAsync(new InteractionCapturePlan("unsafe", [new InteractionActionDefinition(InteractionActionType.ClickSelector, "form.checkout")])));
    }

    private static async Task<InteractionEvidence> CaptureAsync(InteractionCapturePlan plan)
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "interaction-test-" + Guid.NewGuid().ToString("N"));
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        return await new InteractionCaptureService(repoRoot, new FixtureReferenceBrowser())
            .CaptureAsync(
                projectRoot,
                new BrowserPageSession("interaction", "home", new Uri(fixturePath).AbsoluteUri),
                ViewportDefinition.Defaults.Single(viewport => viewport.Id == "mobile-390"),
                new CapturePolicy(),
                plan,
                CancellationToken.None);
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
