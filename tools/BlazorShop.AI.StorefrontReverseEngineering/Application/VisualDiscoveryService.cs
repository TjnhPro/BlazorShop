using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed partial class VisualDiscoveryService
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;
    private readonly IReferenceBrowser browser;

    public VisualDiscoveryService(string repoRoot, IReferenceBrowser browser)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
        this.browser = browser;
    }

    public async Task<DiscoveryResult> DiscoverAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var configuration = await store.ReadJsonAsync<VisualProjectConfiguration>(ArtifactPath.Create("configuration.json"), "configuration", cancellationToken);

        var discovering = VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.Discovering);
        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", discovering, cancellationToken);

        var session = new BrowserPageSession(project.ProjectId, "home", project.ReferenceUrl);
        BrowserCaptureResult capture;
        try
        {
            capture = await browser.CaptureAsync(session, configuration.Viewports.First(), configuration.CapturePolicy, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException)
        {
            capture = new BrowserCaptureResult("failed", "failed", 0, 0, 0, 0, "", [], [], [], [], [exception.Message]);
        }

        var blockers = DetectBlockers(project.ReferenceUrl, capture, configuration.CapturePolicy).ToArray();
        var profile = BuildProfile(project, capture);
        var reconnaissance = new ReconnaissanceReport(
            "1.0",
            "reconnaissance",
            $"reconnaissance-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            project.ReferenceUrl,
            blockers,
            capture.Warnings,
            configuration.CapturePolicy.MaximumPages);
        var capturePlan = new CapturePlan(
            "1.0",
            "capture-plan",
            $"capture-plan-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            [new CapturePlanPage("home", project.ReferenceUrl, "Home")],
            configuration.Viewports);

        await store.WriteJsonAsync(ArtifactPath.Create("discovery/site-profile.json"), "reference-site-profile", profile, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("discovery/reconnaissance.json"), "reconnaissance", reconnaissance, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("discovery/capture-plan.json"), "capture-plan", capturePlan, cancellationToken);

        var discovered = VisualProjectStatusTransitions.MoveTo(discovering, VisualProjectStatus.Discovered);
        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", discovered, cancellationToken);

        return new DiscoveryResult(profile, reconnaissance, capturePlan);
    }

    private static ReferenceSiteProfile BuildProfile(VisualProject project, BrowserCaptureResult capture)
    {
        var html = capture.DomHtml;
        return new ReferenceSiteProfile(
            "1.0",
            "reference-site-profile",
            $"site-profile-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            project.ReferenceUrl,
            ExtractTitle(html),
            ExtractCanonical(html),
            ExtractLanguage(html),
            ExtractMetaViewport(html),
            capture.DocumentWidth,
            capture.DocumentHeight);
    }

    private static IEnumerable<ReconnaissanceBlocker> DetectBlockers(string sourceUrl, BrowserCaptureResult capture, CapturePolicy policy)
    {
        if (capture.CaptureMethod == "failed")
        {
            yield return new("navigation-failure", "blocking", "Reference page could not be opened by the configured browser.");
        }

        if (!sourceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !sourceUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("unsupported-protocol", "blocking", "Reference URL uses an unsupported protocol.");
        }

        if (!capture.DomHtml.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("non-html-response", "blocking", "Reference capture did not return an HTML document.");
        }

        if (capture.DomHtml.Contains("robots", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("robots-warning", "warning", "Reference page includes a robots/crawler warning marker.");
        }

        if (capture.DomHtml.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
            capture.DomHtml.Contains("modal", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("overlay-detected", "warning", "Cookie banner or modal overlay marker detected.");
        }

        if (capture.DomHtml.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            capture.DomHtml.Contains("sign in", StringComparison.OrdinalIgnoreCase))
        {
            yield return new("authentication-wall", "blocking", "Authentication wall marker detected.");
        }

        if (capture.DocumentHeight >= policy.MaximumPageHeight)
        {
            yield return new("excessive-page-height", "warning", "Document height reached the capture policy maximum.");
        }
    }

    private static string? ExtractTitle(string html) => Extract(html, TitleRegex());

    private static string? ExtractCanonical(string html) => Extract(html, CanonicalRegex());

    private static string? ExtractLanguage(string html) => Extract(html, LanguageRegex());

    private static string? ExtractMetaViewport(string html) => Extract(html, ViewportRegex());

    private static string? Extract(string html, Regex regex)
    {
        var match = regex.Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<link[^>]+rel=[\"']canonical[\"'][^>]+href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalRegex();

    [GeneratedRegex("<html[^>]+lang=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LanguageRegex();

    [GeneratedRegex("<meta[^>]+name=[\"']viewport[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ViewportRegex();
}

public sealed record DiscoveryResult(
    ReferenceSiteProfile SiteProfile,
    ReconnaissanceReport Reconnaissance,
    CapturePlan CapturePlan);
