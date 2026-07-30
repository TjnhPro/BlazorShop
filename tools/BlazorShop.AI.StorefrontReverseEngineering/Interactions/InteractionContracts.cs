namespace BlazorShop.AI.StorefrontReverseEngineering.Interactions;

public sealed record InteractionCapturePlan(
    string StateName,
    IReadOnlyList<InteractionActionDefinition> Actions,
    bool MissingSelectorIsBlocking = false);

public sealed record InteractionActionDefinition(
    InteractionActionType Type,
    string? Selector = null,
    int WaitMilliseconds = 0);

public enum InteractionActionType
{
    ClickSelector,
    HoverSelector,
    FocusSelector,
    ScrollToSelector,
    Wait
}

public enum InteractionModel
{
    Static,
    ClickDriven,
    HoverDriven,
    ScrollDriven,
    TimeDriven,
    Mixed,
    Unknown
}

public sealed record InteractionEvidence(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string ViewportId,
    string StateName,
    InteractionModel InteractionModel,
    string BeforeScreenshotPath,
    string AfterScreenshotPath,
    string BeforeDomPath,
    string AfterDomPath,
    string BeforeStylesPath,
    string AfterStylesPath,
    bool DomChanged,
    bool StyleChanged,
    bool ScreenshotChanged,
    string ScreenshotDiffHash,
    IReadOnlyList<string> ChangedElementEvidenceIds,
    string DomDiffSummary,
    string StyleDiffSummary,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
