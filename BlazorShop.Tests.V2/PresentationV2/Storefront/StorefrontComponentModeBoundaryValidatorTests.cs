namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontComponentModeBoundaryValidatorTests
{
    private readonly StorefrontComponentModeBoundaryValidator validator = new();

    [Theory]
    [MemberData(nameof(RepositoryProfiles))]
    internal void RepositoryModeProjectsPassTheirBoundaryProfiles(StorefrontComponentModeProfile profile)
    {
        var violations = this.validator.Validate(profile);

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(PositiveFixtures))]
    internal void PositiveFixturesPass(StorefrontComponentModeProfile profile, IReadOnlyDictionary<string, string> files)
    {
        using var fixture = StorefrontComponentModeFixture.Create(profile, files);

        var violations = this.validator.Validate(fixture.Profile);

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(NegativeFixtures))]
    internal void NegativeFixturesFailWithActionableViolations(
        StorefrontComponentModeProfile profile,
        IReadOnlyDictionary<string, string> files,
        string expectedForbidden)
    {
        using var fixture = StorefrontComponentModeFixture.Create(profile, files);

        var violations = this.validator.Validate(fixture.Profile);

        Assert.Contains(violations, violation => violation.Forbidden.Contains(expectedForbidden, StringComparison.Ordinal));
        Assert.All(violations, violation =>
        {
            Assert.False(string.IsNullOrWhiteSpace(violation.RelativePath));
            Assert.False(string.IsNullOrWhiteSpace(violation.Owner));
            Assert.False(string.IsNullOrWhiteSpace(violation.Remediation));
        });
    }

    public static IEnumerable<object[]> RepositoryProfiles()
    {
        yield return [StorefrontComponentModeProfiles.Ssr(RepositoryRoot)];
        yield return [StorefrontComponentModeProfiles.Hybrid(RepositoryRoot)];
        yield return [StorefrontComponentModeProfiles.WasmHost(RepositoryRoot)];
    }

    public static IEnumerable<object[]> PositiveFixtures()
    {
        yield return
        [
            SsrFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Ssr.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj"),
                ["Components/BrandBlock.razor"] = "<section data-storefront-brand></section>",
            },
        ];
        yield return
        [
            HybridFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Hybrid.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                    "../BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj"),
                ["Components/InteractiveBridge.razor"] = """
                    @using BlazorShop.Storefront.Components.WasmHost
                    @rendermode InteractiveWebAssembly
                    <section data-storefront-hybrid></section>
                    """,
            },
        ];
        yield return
        [
            WasmHostFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.WasmHost.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj"),
                ["Components/CartControllerHost.razor"] = """
                    @using Microsoft.JSInterop
                    @using BlazorShop.Storefront.Browser
                    @inject IJSRuntime JS
                    <button data-storefront-action @onclick="OnClick"></button>
                    @code {
                        [Parameter] public EventCallback OnClick { get; set; }
                        [Inject] public IStorefrontBrowserCartController? CartController { get; set; }
                    }
                    """,
            },
        ];
    }

    public static IEnumerable<object[]> NegativeFixtures()
    {
        yield return Negative(
            SsrFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Ssr.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj"),
                ["Components/BadInterop.razor"] = "@inject IJSRuntime JS",
            },
            "IJSRuntime");
        yield return Negative(
            SsrFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Ssr.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                    "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj"),
            },
            "ProjectReference:../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj");
        yield return Negative(
            SsrFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Ssr.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj"),
                ["Components/BadRenderMode.razor"] = "@rendermode InteractiveWebAssembly",
            },
            "@rendermode");
        yield return Negative(
            HybridFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Hybrid.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                    "../BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
                    "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj"),
            },
            "ProjectReference:../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj");
        yield return Negative(
            HybridFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Hybrid.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                    "../BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj"),
                ["Components/BadHttp.razor"] = "@inject HttpClient Http",
            },
            "HttpClient");
        yield return Negative(
            HybridFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.Hybrid.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                    "../BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj"),
                ["Components/BadBrowserController.razor"] = "@inject IStorefrontBrowserCartController CartController",
            },
            "IStorefrontBrowser");
        yield return Negative(
            WasmHostFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.WasmHost.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj",
                    "../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj"),
            },
            "ProjectReference:../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");
        yield return Negative(
            WasmHostFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.WasmHost.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj"),
                ["Components/BadHttp.razor"] = "@inject HttpClient Http",
            },
            "HttpClient");
        yield return Negative(
            WasmHostFixtureProfile(),
            new Dictionary<string, string>
            {
                ["Positive.WasmHost.csproj"] = Project(
                    "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj"),
                ["Components/BadApi.razor"] = "private const string Path = \"/api/storefront/stores/default/cart\";",
            },
            "api/storefront");

        foreach (var profile in new[] { SsrFixtureProfile(), HybridFixtureProfile(), WasmHostFixtureProfile() })
        {
            yield return Negative(
                profile,
                new Dictionary<string, string>
                {
                    [profile.RelativeProjectPath] = Project(
                        "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                        "../BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"),
                },
                "BlazorShop.Storefront.V2");
            yield return Negative(
                profile,
                new Dictionary<string, string>
                {
                    [profile.RelativeProjectPath] = Project(
                        "../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                        "../../BlazorShop.Application/BlazorShop.Application.csproj"),
                },
                "BlazorShop.Application");
        }
    }

    private static object[] Negative(StorefrontComponentModeProfile profile, Dictionary<string, string> files, string expectedForbidden)
    {
        return [profile, files, expectedForbidden];
    }

    private static StorefrontComponentModeProfile SsrFixtureProfile()
    {
        return FixtureProfile(
            "Components.Ssr",
            "Positive.Ssr",
            "Positive.Ssr.csproj",
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
            ],
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
            ],
            [
                "HttpClient",
                "IHttpClientFactory",
                "IJSRuntime",
                "JSImport",
                "@rendermode",
                "InteractiveWebAssembly",
                "InteractiveServer",
                "\"/api/",
                "'/api/",
                "api/storefront",
                "localhost:",
                "CommerceNodeBaseUrl",
                "StorefrontLocalApiClient",
            ]);
    }

    private static StorefrontComponentModeProfile HybridFixtureProfile()
    {
        return FixtureProfile(
            "Components.Hybrid",
            "Positive.Hybrid",
            "Positive.Hybrid.csproj",
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                "BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
            ],
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                "BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
            ],
            [
                "HttpClient",
                "IHttpClientFactory",
                "IJSRuntime",
                "JSImport",
                "\"/api/",
                "'/api/",
                "api/storefront",
                "localhost:",
                "CommerceNodeBaseUrl",
                "StorefrontLocalApiClient",
                "IStorefrontBrowser",
            ]);
    }

    private static StorefrontComponentModeProfile WasmHostFixtureProfile()
    {
        return FixtureProfile(
            "Components.WasmHost",
            "Positive.WasmHost",
            "Positive.WasmHost.csproj",
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj",
            ],
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj",
            ],
            [
                "HttpClient",
                "IHttpClientFactory",
                "HttpContext",
                "IHttpContextAccessor",
                "\"/api/",
                "'/api/",
                "api/storefront",
                "localhost:",
                "CommerceNodeBaseUrl",
                "BlazorShop.Storefront.Presentation",
                "IStorefrontRuntime",
                "IStorefrontCatalogClient",
                "IStorefrontCartClient",
                "IStorefrontCheckoutClient",
                "IStorefrontCustomerClient",
            ]);
    }

    private static StorefrontComponentModeProfile FixtureProfile(
        string owner,
        string relativeProjectDirectory,
        string relativeProjectPath,
        string[] requiredProjectReferenceFragments,
        string[] allowedProjectReferenceFragments,
        string[] forbiddenSourceTokens)
    {
        return new StorefrontComponentModeProfile(
            owner,
            string.Empty,
            relativeProjectDirectory,
            relativeProjectPath,
            requiredProjectReferenceFragments.ToHashSet(StringComparer.OrdinalIgnoreCase),
            allowedProjectReferenceFragments.ToHashSet(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            forbiddenSourceTokens.ToHashSet(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            $"{owner} fixture project references must stay inside the allowlist.",
            $"{owner} fixture source must stay inside the mode boundary.");
    }

    private static string Project(params string[] references)
    {
        var projectReferences = string.Join(
            Environment.NewLine,
            references.Select(reference => $"    <ProjectReference Include=\"{reference}\" />"));

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <ItemGroup>
            {{projectReferences}}
              </ItemGroup>
            </Project>
            """;
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private sealed class StorefrontComponentModeFixture : IDisposable
    {
        private StorefrontComponentModeFixture(string root, StorefrontComponentModeProfile profile)
        {
            this.Root = root;
            this.Profile = profile;
        }

        public string Root { get; }

        public StorefrontComponentModeProfile Profile { get; }

        public static StorefrontComponentModeFixture Create(
            StorefrontComponentModeProfile template,
            IReadOnlyDictionary<string, string> files)
        {
            var root = Path.Combine(Path.GetTempPath(), "storefront-component-mode-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            foreach (var (relativePath, contents) in files)
            {
                var absolutePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? root);
                File.WriteAllText(absolutePath, contents);
            }

            var profile = template with
            {
                RepositoryRoot = root,
                RelativeProjectDirectory = Path.GetDirectoryName(template.RelativeProjectPath)?.Replace('\\', '/') ?? string.Empty,
            };

            return new StorefrontComponentModeFixture(root, profile);
        }

        public void Dispose()
        {
            if (Directory.Exists(this.Root))
            {
                Directory.Delete(this.Root, recursive: true);
            }
        }
    }
}
