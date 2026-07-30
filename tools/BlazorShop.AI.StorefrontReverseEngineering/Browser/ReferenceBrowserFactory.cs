namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public static class ReferenceBrowserFactory
{
    public static IReferenceBrowser Create(string repoRoot, string sourceUrl)
    {
        var uri = new Uri(sourceUrl);
        if (uri.Scheme == "file")
        {
            return new FixtureReferenceBrowser();
        }

        if (uri.Host.EndsWith(".test", StringComparison.OrdinalIgnoreCase))
        {
            return new SyntheticReferenceBrowser();
        }

        return new PlaywrightReferenceBrowser();
    }
}
