namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontManualClientRetirementGuardrailTests
{
    private static readonly string[] ManualTransportTokens =
    [
        "StorefrontApiClient",
        "EnableLegacyFallback",
        "AddHttpClient<StorefrontApiClient>",
        "LegacyCatalogBaseRoute",
        "LegacySeoSettingsRoute"
    ];

    private static readonly string[] PresentationClientContractImplementations =
    [
        ": IStorefrontAddressClient",
        ": IStorefrontCartClient",
        ": IStorefrontCatalogClient",
        ": IStorefrontCheckoutClient",
        ": IStorefrontConsentClient",
        ": IStorefrontContentClient",
        ": IStorefrontCustomerClient",
        ": IStorefrontPaymentClient",
        ": IStorefrontStoreConfigurationClient"
    ];

    private static readonly string[] ManualCommerceNodeRouteTokens =
    [
        "api/storefront/stores/{",
        "/api/storefront/stores/",
        "\"api/storefront/stores",
        "LegacyCatalogBaseRoute",
        "LegacySeoSettingsRoute",
        "/api/public/catalog",
        "/api/seo/settings"
    ];

    [Fact]
    public void MvpBlocker_StorefrontV2ManualClientTransport_MustBeRetired()
    {
        var offenders = FindOffenders(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
            ManualTransportTokens,
            [".cs", ".csproj", ".json"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 must retire manual StorefrontApiClient transport before release.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_StorefrontV2Classes_MustNotImplementPresentationClientContracts()
    {
        var offenders = FindOffenders(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
            PresentationClientContractImplementations,
            [".cs"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 must not implement Presentation IStorefront*Client contracts.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void MvpBlocker_StorefrontV2Host_MustNotConstructCommerceNodeStorefrontRoutesDirectly()
    {
        var offenders = FindOffenders(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
            ManualCommerceNodeRouteTokens,
            [".cs"]);

        Assert.True(
            offenders.Length == 0,
            $"MVP blocker: Storefront V2 may expose same-origin BFF endpoints, but must not construct Commerce Node Storefront API transport routes directly.{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static string[] FindOffenders(string relativeRoot, IReadOnlyCollection<string> forbiddenTokens, IReadOnlyCollection<string> extensions)
    {
        return Directory
            .EnumerateFiles(RepositoryPath(relativeRoot), "*.*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path =>
            {
                var source = File.ReadAllText(path);
                return forbiddenTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{NormalizePath(Path.GetRelativePath(RepositoryRoot(), path))}: {token}");
            })
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                && File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
