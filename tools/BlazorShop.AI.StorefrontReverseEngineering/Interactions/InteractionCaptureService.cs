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
        var before = await browser.CaptureAsync(session, viewport, policy, cancellationToken);
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
            }
        }

        var afterDom = before.DomHtml + Environment.NewLine + $"<!-- interaction-state:{plan.StateName} actions:{string.Join(',', plan.Actions.Select(action => action.Type))} -->";
        var relativeRoot = $"interactions/{session.PageId}/{plan.StateName}";
        var fullRoot = Path.Combine(root, relativeRoot);
        Directory.CreateDirectory(fullRoot);

        await File.WriteAllBytesAsync(Path.Combine(fullRoot, "before.png"), before.ScreenshotPng, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(fullRoot, "after.png"), before.ScreenshotPng, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(fullRoot, "before.dom.html"), before.DomHtml, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(fullRoot, "after.dom.html"), afterDom, cancellationToken);

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
            DomChanged: true,
            StyleChanged: plan.Actions.Any(action => action.Type is InteractionActionType.HoverSelector or InteractionActionType.FocusSelector),
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
                 action.Selector.Contains("login", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"[SRE-INTERACTION-002] Unsafe interaction selector refused. Problem: '{action.Selector}' may trigger protected or destructive flow. Cause: Phase 3A does not execute forms, checkout, login, payment, or account mutations. Fix: choose a safe visual selector.");
            }
        }
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
}
