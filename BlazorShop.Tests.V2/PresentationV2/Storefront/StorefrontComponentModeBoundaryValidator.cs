namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Xml.Linq;

internal sealed class StorefrontComponentModeBoundaryValidator
{
    private static readonly string[] SourceExtensions =
    [
        ".cs",
        ".razor",
        ".cshtml",
        ".js",
        ".mjs",
        ".ts",
        ".json",
        ".yaml",
        ".yml",
        ".css",
        ".scss",
        ".sass",
        ".less",
    ];

    private static readonly string[] IgnoredDirectoryNames =
    [
        "bin",
        "obj",
        "node_modules",
        "artifacts",
        "packages",
        "pkg",
    ];

    public IReadOnlyList<StorefrontComponentModeBoundaryViolation> Validate(StorefrontComponentModeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var violations = new List<StorefrontComponentModeBoundaryViolation>();
        ValidateProjectReferences(profile, violations);
        ValidatePackageReferences(profile, violations);
        ValidateSourceTokens(profile, violations);

        return violations
            .OrderBy(violation => violation.RelativePath, StringComparer.Ordinal)
            .ThenBy(violation => violation.Forbidden, StringComparer.Ordinal)
            .ThenBy(violation => violation.Owner, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateProjectReferences(
        StorefrontComponentModeProfile profile,
        List<StorefrontComponentModeBoundaryViolation> violations)
    {
        if (!File.Exists(profile.AbsoluteProjectPath))
        {
            violations.Add(StorefrontComponentModeBoundaryViolation.Project(
                profile.RelativeProjectPath,
                "missing project file",
                profile.Owner,
                "Create the mode project before validating its boundary."));
            return;
        }

        var document = XDocument.Load(profile.AbsoluteProjectPath);
        var references = document.Descendants("ProjectReference")
            .Select(element => NormalizePath(element.Attribute("Include")?.Value ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var reference in references)
        {
            var allowed = profile.AllowedProjectReferenceFragments.Any(fragment =>
                reference.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            if (allowed)
            {
                continue;
            }

            violations.Add(StorefrontComponentModeBoundaryViolation.Project(
                profile.RelativeProjectPath,
                $"ProjectReference:{reference}",
                profile.Owner,
                profile.ProjectReferenceRemediation));
        }

        foreach (var fragment in profile.RequiredProjectReferenceFragments.OrderBy(value => value, StringComparer.Ordinal))
        {
            var present = references.Any(reference =>
                reference.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            if (present)
            {
                continue;
            }

            violations.Add(StorefrontComponentModeBoundaryViolation.Project(
                profile.RelativeProjectPath,
                $"MissingProjectReference:{fragment}",
                profile.Owner,
                "Mode projects must keep their exact direct project-reference allowlist."));
        }
    }

    private static void ValidatePackageReferences(
        StorefrontComponentModeProfile profile,
        List<StorefrontComponentModeBoundaryViolation> violations)
    {
        if (!File.Exists(profile.AbsoluteProjectPath))
        {
            return;
        }

        var document = XDocument.Load(profile.AbsoluteProjectPath);
        foreach (var package in document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            if (profile.AllowedPackageReferences.Contains(package, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            violations.Add(StorefrontComponentModeBoundaryViolation.Project(
                profile.RelativeProjectPath,
                $"PackageReference:{package}",
                profile.Owner,
                "Mode project package references must be explicitly allowlisted by profile."));
        }
    }

    private static void ValidateSourceTokens(
        StorefrontComponentModeProfile profile,
        List<StorefrontComponentModeBoundaryViolation> violations)
    {
        if (!Directory.Exists(profile.AbsoluteProjectDirectory))
        {
            violations.Add(StorefrontComponentModeBoundaryViolation.Source(
                profile.RelativeProjectDirectory,
                "missing project directory",
                profile.Owner,
                "Create the mode project directory before validating source."));
            return;
        }

        foreach (var file in EnumerateSourceFiles(profile.AbsoluteProjectDirectory))
        {
            var source = File.ReadAllText(file);
            var relativePath = ToProfileRelativePath(profile, file);

            foreach (var token in profile.ForbiddenSourceTokens.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add(StorefrontComponentModeBoundaryViolation.Source(
                    relativePath,
                    token,
                    profile.Owner,
                    profile.SourceTokenRemediation));
            }
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory)
    {
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(file => SourceExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(file => !IsIgnoredPath(file))
            .OrderBy(file => file, StringComparer.Ordinal);
    }

    private static bool IsIgnoredPath(string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => IgnoredDirectoryNames.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    private static string ToProfileRelativePath(StorefrontComponentModeProfile profile, string absolutePath)
    {
        return NormalizePath(Path.GetRelativePath(profile.RepositoryRoot, absolutePath));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

internal static class StorefrontComponentModeProfiles
{
    private static readonly string[] SsrForbiddenSourceTokens =
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
    ];

    private static readonly string[] HybridForbiddenSourceTokens =
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
    ];

    private static readonly string[] WasmHostForbiddenSourceTokens =
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
    ];

    private static readonly string[] HybridAllowedSourceTokens =
    [
        "@rendermode",
        "InteractiveWebAssembly",
        "BlazorShop.Storefront.Components.WasmHost",
    ];

    private static readonly string[] WasmHostAllowedSourceTokens =
    [
        "IJSRuntime",
        "EventCallback",
        "BlazorShop.Storefront.Browser",
    ];

    public static StorefrontComponentModeProfile Ssr(string repositoryRoot)
    {
        return Create(
            "Components.Ssr",
            repositoryRoot,
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj",
            requiredProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
            ],
            allowedProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
            ],
            forbiddenSourceTokens: SsrForbiddenSourceTokens,
            allowedSourceTokens: [],
            projectReferenceRemediation: "SSR components may reference only base Components and Presentation.",
            sourceTokenRemediation: "Move browser, runtime, client, API, JS interop, and render-mode behavior out of SSR components.");
    }

    public static StorefrontComponentModeProfile Hybrid(string repositoryRoot)
    {
        return Create(
            "Components.Hybrid",
            repositoryRoot,
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj",
            requiredProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                "BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
            ],
            allowedProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                "BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
            ],
            forbiddenSourceTokens: HybridForbiddenSourceTokens,
            allowedSourceTokens: HybridAllowedSourceTokens,
            projectReferenceRemediation: "The transitional Components.Hybrid project may reference only base Components, Presentation, and Components.WasmHost until H2 decides its permanent role. This is not the semantic definition of Hybrid mode.",
            sourceTokenRemediation: "Keep the transitional Components.Hybrid project free of direct browser transport, API calls, HttpClient, and JS interop behavior; H2 owns any permanent Hybrid runtime pattern changes.");
    }

    public static StorefrontComponentModeProfile WasmHost(string repositoryRoot)
    {
        return Create(
            "Components.WasmHost",
            repositoryRoot,
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj",
            requiredProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj",
            ],
            allowedProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                "BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj",
            ],
            forbiddenSourceTokens: WasmHostForbiddenSourceTokens,
            allowedSourceTokens: WasmHostAllowedSourceTokens,
            projectReferenceRemediation: "WasmHost components may reference only base Components and Browser.",
            sourceTokenRemediation: "Route browser work through Browser controllers and same-origin Presentation/BFF endpoints; do not inject server/runtime clients or call APIs directly.");
    }

    private static StorefrontComponentModeProfile Create(
        string owner,
        string repositoryRoot,
        string relativeProjectDirectory,
        string relativeProjectPath,
        string[] requiredProjectReferenceFragments,
        string[] allowedProjectReferenceFragments,
        string[] forbiddenSourceTokens,
        string[] allowedSourceTokens,
        string projectReferenceRemediation,
        string sourceTokenRemediation)
    {
        return new StorefrontComponentModeProfile(
            owner,
            repositoryRoot,
            relativeProjectDirectory,
            relativeProjectPath,
            requiredProjectReferenceFragments.ToHashSet(StringComparer.OrdinalIgnoreCase),
            allowedProjectReferenceFragments.ToHashSet(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            forbiddenSourceTokens.ToHashSet(StringComparer.Ordinal),
            allowedSourceTokens.ToHashSet(StringComparer.Ordinal),
            projectReferenceRemediation,
            sourceTokenRemediation);
    }
}

internal sealed record StorefrontComponentModeProfile(
    string Owner,
    string RepositoryRoot,
    string RelativeProjectDirectory,
    string RelativeProjectPath,
    IReadOnlySet<string> RequiredProjectReferenceFragments,
    IReadOnlySet<string> AllowedProjectReferenceFragments,
    IReadOnlySet<string> AllowedPackageReferences,
    IReadOnlySet<string> ForbiddenSourceTokens,
    IReadOnlySet<string> AllowedSourceTokens,
    string ProjectReferenceRemediation,
    string SourceTokenRemediation)
{
    public string AbsoluteProjectDirectory => Path.Combine(
        this.RepositoryRoot,
        this.RelativeProjectDirectory.Replace('/', Path.DirectorySeparatorChar));

    public string AbsoluteProjectPath => Path.Combine(
        this.RepositoryRoot,
        this.RelativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
}

internal sealed record StorefrontComponentModeBoundaryViolation(
    string RelativePath,
    string Forbidden,
    string Owner,
    string Remediation,
    StorefrontComponentModeBoundaryViolationKind Kind)
{
    public static StorefrontComponentModeBoundaryViolation Project(
        string relativePath,
        string forbidden,
        string owner,
        string remediation)
    {
        return new StorefrontComponentModeBoundaryViolation(
            relativePath,
            forbidden,
            owner,
            remediation,
            StorefrontComponentModeBoundaryViolationKind.Project);
    }

    public static StorefrontComponentModeBoundaryViolation Source(
        string relativePath,
        string forbidden,
        string owner,
        string remediation)
    {
        return new StorefrontComponentModeBoundaryViolation(
            relativePath,
            forbidden,
            owner,
            remediation,
            StorefrontComponentModeBoundaryViolationKind.Source);
    }
}

internal enum StorefrontComponentModeBoundaryViolationKind
{
    Project,
    Source,
}
