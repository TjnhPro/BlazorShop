using System.Security.Cryptography;
using System.Text;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Interactions;

public sealed class InteractionCaptureService
{
    private readonly IReferenceBrowser browser;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public InteractionCaptureService(string repoRoot, IReferenceBrowser browser)
    {
        this.browser = browser;
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<InteractionEvidence> CaptureAsync(
        string projectRoot,
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        InteractionCapturePlan plan,
        CancellationToken cancellationToken)
    {
        ValidateSafeActions(plan);

        var root = resolver.ResolveRoot(projectRoot);
        await using var browserSession = await browser.OpenSessionAsync(session, viewport, policy, cancellationToken);
        await browserSession.NavigateAsync(cancellationToken);
        await browserSession.StabilizeAsync(cancellationToken);
        var before = await browserSession.CaptureCurrentStateAsync(cancellationToken);
        var warnings = new List<string>();
        var errors = new List<string>();

        foreach (var action in plan.Actions)
        {
            if (action.Type != InteractionActionType.Wait &&
                !SelectorExists(before.DomHtml, action.Selector))
            {
                var message = $"Selector '{action.Selector}' was not found for {action.Type}.";
                if (plan.MissingSelectorIsBlocking)
                {
                    errors.Add(message);
                }
                else
                {
                    warnings.Add(message);
                }

                continue;
            }

            var actionResult = await browserSession.ExecuteAsync(ToBrowserAction(action), cancellationToken);
            warnings.AddRange(actionResult.Warnings);
        }

        var after = errors.Count == 0
            ? await browserSession.CaptureCurrentStateAsync(cancellationToken)
            : before;
        var beforeStylesJson = Serialize(before.Styles);
        var afterStylesJson = Serialize(after.Styles);
        var changedEvidenceIds = FindChangedEvidenceIds(before.Styles, after.Styles);
        var screenshotChanged = !CryptographicOperations.FixedTimeEquals(Sha256(before.ScreenshotPng), Sha256(after.ScreenshotPng));
        var domChanged = !string.Equals(before.DomHtml, after.DomHtml, StringComparison.Ordinal);
        var styleChanged = !string.Equals(beforeStylesJson, afterStylesJson, StringComparison.Ordinal);
        var relativeRoot = $"interactions/{session.PageId}/{plan.StateName}";
        var fullRoot = Path.Combine(root, relativeRoot);
        Directory.CreateDirectory(fullRoot);

        await File.WriteAllBytesAsync(Path.Combine(fullRoot, "before.png"), before.ScreenshotPng, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(fullRoot, "after.png"), after.ScreenshotPng, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(fullRoot, "before.dom.html"), before.DomHtml, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(fullRoot, "after.dom.html"), after.DomHtml, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(fullRoot, "before.styles.json"), beforeStylesJson, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(fullRoot, "after.styles.json"), afterStylesJson, cancellationToken);

        var evidence = new InteractionEvidence(
            "1.0",
            "interaction-evidence",
            $"interaction-{session.ProjectId}-{session.PageId}-{viewport.Id}-{plan.StateName}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            viewport.Id,
            plan.StateName,
            Classify(plan),
            $"{relativeRoot}/before.png",
            $"{relativeRoot}/after.png",
            $"{relativeRoot}/before.dom.html",
            $"{relativeRoot}/after.dom.html",
            $"{relativeRoot}/before.styles.json",
            $"{relativeRoot}/after.styles.json",
            DomChanged: domChanged,
            StyleChanged: styleChanged,
            ScreenshotChanged: screenshotChanged,
            ScreenshotDiffHash: Convert.ToHexString(Sha256(before.ScreenshotPng.Concat(after.ScreenshotPng).ToArray())),
            ChangedElementEvidenceIds: changedEvidenceIds,
            DomDiffSummary: domChanged ? "DOM content changed after interaction." : "DOM content did not change after interaction.",
            StyleDiffSummary: styleChanged ? "Computed style evidence changed after interaction." : "Computed style evidence did not change after interaction.",
            warnings,
            errors);

        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/interaction-evidence.json"), "interaction-evidence", evidence, cancellationToken);
        return evidence;
    }

    private static void ValidateSafeActions(InteractionCapturePlan plan)
    {
        foreach (var action in plan.Actions)
        {
            if (action.Type != InteractionActionType.Wait && string.IsNullOrWhiteSpace(action.Selector))
            {
                throw new InvalidOperationException("[SRE-INTERACTION-001] Interaction selector is required. Problem: a configured action has no selector. Cause: Phase 3A only runs explicit safe selectors. Fix: provide a selector or use a wait action.");
            }

            if (action.Selector is not null &&
                (action.Selector.Contains("form", StringComparison.OrdinalIgnoreCase) ||
                 action.Selector.Contains("checkout", StringComparison.OrdinalIgnoreCase) ||
                 action.Selector.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
                 action.Selector.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                 action.Selector.Contains("account", StringComparison.OrdinalIgnoreCase) ||
                 action.Selector.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                 action.Selector.Contains("purchase", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"[SRE-INTERACTION-002] Unsafe interaction selector refused. Problem: '{action.Selector}' may trigger protected or destructive flow. Cause: Phase 3A does not execute forms, checkout, login, account, purchase, delete, payment, or account mutations. Fix: choose a safe visual selector.");
            }
        }
    }

    private static BrowserSessionAction ToBrowserAction(InteractionActionDefinition action)
    {
        return action.Type switch
        {
            InteractionActionType.ClickSelector => new BrowserSessionAction("click-selector", action.Selector, action.WaitMilliseconds),
            InteractionActionType.HoverSelector => new BrowserSessionAction("hover-selector", action.Selector, action.WaitMilliseconds),
            InteractionActionType.FocusSelector => new BrowserSessionAction("focus-selector", action.Selector, action.WaitMilliseconds),
            InteractionActionType.ScrollToSelector => new BrowserSessionAction("scroll-to-selector", action.Selector, action.WaitMilliseconds),
            InteractionActionType.Wait => new BrowserSessionAction("wait", DelayMilliseconds: Math.Max(0, action.WaitMilliseconds)),
            _ => new BrowserSessionAction("unsupported")
        };
    }

    private static bool SelectorExists(string html, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return true;
        }

        if (selector.StartsWith(".", StringComparison.Ordinal))
        {
            return html.Contains(selector[1..], StringComparison.OrdinalIgnoreCase);
        }

        if (selector.StartsWith("#", StringComparison.Ordinal))
        {
            return html.Contains($"id=\"{selector[1..]}\"", StringComparison.OrdinalIgnoreCase) ||
                   html.Contains($"id='{selector[1..]}'", StringComparison.OrdinalIgnoreCase);
        }

        return html.Contains("<" + selector.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0], StringComparison.OrdinalIgnoreCase);
    }

    private static InteractionModel Classify(InteractionCapturePlan plan)
    {
        var models = plan.Actions.Select(action => action.Type switch
        {
            InteractionActionType.ClickSelector => InteractionModel.ClickDriven,
            InteractionActionType.HoverSelector => InteractionModel.HoverDriven,
            InteractionActionType.FocusSelector => InteractionModel.ClickDriven,
            InteractionActionType.ScrollToSelector => InteractionModel.ScrollDriven,
            InteractionActionType.Wait => InteractionModel.TimeDriven,
            _ => InteractionModel.Unknown
        }).Distinct().ToArray();

        if (models.Length == 0)
        {
            return InteractionModel.Static;
        }

        return models.Length == 1 ? models[0] : InteractionModel.Mixed;
    }

    private static IReadOnlyList<string> FindChangedEvidenceIds(
        IReadOnlyList<ComputedStyleSample> before,
        IReadOnlyList<ComputedStyleSample> after)
    {
        var beforeMap = before.ToDictionary(style => style.EvidenceId ?? style.Selector, style => Serialize(style.Properties), StringComparer.Ordinal);
        return after
            .Where(style => !beforeMap.TryGetValue(style.EvidenceId ?? style.Selector, out var beforeStyle) ||
                            !string.Equals(beforeStyle, Serialize(style.Properties), StringComparison.Ordinal))
            .Select(style => style.EvidenceId ?? style.Selector)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static byte[] Sha256(byte[] value) => SHA256.HashData(value);

    private static string Serialize<TValue>(TValue value) =>
        System.Text.Json.JsonSerializer.Serialize(value, VisualJson.Options) + Environment.NewLine;
}
