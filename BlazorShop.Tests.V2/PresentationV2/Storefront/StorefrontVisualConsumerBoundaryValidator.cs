namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Xml.Linq;

internal sealed class StorefrontVisualConsumerBoundaryValidator
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
    ];

    private static readonly string[] ForbiddenProjectReferenceFragments =
    [
        "BlazorShop.Application",
        "BlazorShop.Domain",
        "BlazorShop.Infrastructure",
        "BlazorShop.CommerceNode.API",
        "BlazorShop.ControlPlane.API",
        "BlazorShop.ControlPlane.Web",
        "BlazorShop.Web.SharedV2",
    ];

    private static readonly string[] ForbiddenSourceTokens =
    [
        "HttpClient",
        "IHttpClientFactory",
        "fetch(",
        "XMLHttpRequest",
        "/api/storefront/",
        "/api/cart",
        "/api/checkout",
        "/api/consent",
        "/api/product-selection-preview",
        "blazorshop-antiforgery-token",
        "blazorshop-antiforgery-header",
        "[Inject] IServiceProvider",
        "GetRequiredService<",
        "GetService<",
        ": IStorefront",
        ": StorefrontRuntime",
        "StorefrontApiClient",
        "ManualStorefront",
    ];

    private static readonly string[] ForbiddenBrowserCommandTokens =
    [
        ".application.cart.",
        ".application.consent.",
        ".application.productSelection.",
        "application.cart",
        "application.consent",
        "application.productSelection",
        "cart.addLine",
        "cart.updateLine",
        "cart.removeLine",
        "cart.clear",
        "cart.recalculate",
        "productSelection.preview",
        "consent.accept",
        "consent.revoke",
    ];

    private static readonly string[] ForbiddenBrowserPayloadTokens =
    [
        "ProductId:",
        "ProductVariantId:",
        "SelectedAttributes:",
        "CurrencyCode:",
        "productId:",
        "productVariantId:",
        "selectedAttributes:",
        "currencyCode:",
    ];

    private static readonly string[] ForbiddenBrowserBusinessTokens =
    [
        "preview.canAddToCart",
        "preview.stockQuantity",
        "preview.isAvailable",
        "preview.validationMessages",
        "preview.unitPrice",
        "preview.formattedUnitPrice",
        "preview.formattedComparePrice",
        "preview.sku",
        "preview.gtin",
        "preview[\"canAddToCart\"]",
        "preview['canAddToCart']",
        "preview[\"stockQuantity\"]",
        "preview['stockQuantity']",
        "preview[\"isAvailable\"]",
        "preview['isAvailable']",
        "preview[\"validationMessages\"]",
        "preview['validationMessages']",
        "preview[\"unitPrice\"]",
        "preview['unitPrice']",
        "preview[\"formattedUnitPrice\"]",
        "preview['formattedUnitPrice']",
        "preview[\"formattedComparePrice\"]",
        "preview['formattedComparePrice']",
        "preview[\"sku\"]",
        "preview['sku']",
        "preview[\"gtin\"]",
        "preview['gtin']",
    ];

    private static readonly string[] ForbiddenRootFolders =
    [
        "Services",
        "Services/Contracts",
        "Security",
        "Middleware",
        "Endpoints",
        "Configuration",
        "Options",
        "Models",
    ];

    public IReadOnlyList<StorefrontVisualBoundaryViolation> Validate(StorefrontVisualConsumerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var violations = new List<StorefrontVisualBoundaryViolation>();
        ValidateProjectReferences(profile, violations);
        ValidateForbiddenFolders(profile, violations);
        ValidateSource(profile, violations);
        return violations
            .OrderBy(violation => violation.RelativePath, StringComparer.Ordinal)
            .ThenBy(violation => violation.Forbidden, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateProjectReferences(
        StorefrontVisualConsumerProfile profile,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        if (!File.Exists(profile.AbsoluteProjectPath))
        {
            violations.Add(StorefrontVisualBoundaryViolation.Project(
                profile.RelativeProjectPath,
                "missing project file",
                "The visual consumer project must be present before boundary validation can run."));
            return;
        }

        var document = XDocument.Load(profile.AbsoluteProjectPath);
        foreach (var reference in document.Descendants("ProjectReference")
            .Select(element => NormalizePath(element.Attribute("Include")?.Value ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var packageName = Path.GetFileNameWithoutExtension(reference);
            var allowed = profile.AllowedProjectReferenceFragments.Any(fragment =>
                reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)
                || packageName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            if (allowed)
            {
                continue;
            }

            if (ForbiddenProjectReferenceFragments.Any(fragment => reference.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                || reference.Contains("BlazorShop.Storefront.Runtime", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("BlazorShop.Storefront.Client", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("BlazorShop.Storefront.V2", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("BlazorShop.Storefront.", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(StorefrontVisualBoundaryViolation.Project(
                    profile.RelativeProjectPath,
                    $"ProjectReference:{reference}",
                    "Visual consumers should depend on Storefront Presentation/Components only; move runtime/client/backend logic behind Presentation."));
            }
        }

        foreach (var package in document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var isRuntimeOrClient = package.Equals("BlazorShop.Storefront.Runtime", StringComparison.OrdinalIgnoreCase)
                || package.Equals("BlazorShop.Storefront.Client", StringComparison.OrdinalIgnoreCase);
            if (isRuntimeOrClient)
            {
                violations.Add(StorefrontVisualBoundaryViolation.Project(
                    profile.RelativeProjectPath,
                    $"PackageReference:{package}",
                    "Visual consumers must not compile against Runtime/Client packages. Version metadata is allowed only in non-project files such as props or YAML."));
                continue;
            }

            if (profile.AllowedPackageReferences.Contains(package, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (package.StartsWith("BlazorShop.CommerceNode", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("BlazorShop.ControlPlane", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(StorefrontVisualBoundaryViolation.Project(
                    profile.RelativeProjectPath,
                    $"PackageReference:{package}",
                    "Visual consumers should not compile against backend packages; move runtime logic behind Storefront Presentation."));
            }
        }
    }

    private static void ValidateForbiddenFolders(
        StorefrontVisualConsumerProfile profile,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        foreach (var folder in ForbiddenRootFolders)
        {
            var absoluteFolder = Path.Combine(profile.AbsoluteRoot, folder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absoluteFolder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(absoluteFolder, "*.*", SearchOption.AllDirectories)
                .Where(path => SourceExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .Where(path => !IsBuildOutput(path)))
            {
                var relativePath = ToProfileRelativePath(profile, file);
                if (profile.AllowedSourceRelativePaths.Contains(relativePath, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                violations.Add(StorefrontVisualBoundaryViolation.Source(
                    relativePath,
                    $"folder:{folder}",
                    "Application logic folders belong in Storefront Presentation; visual consumers should keep components, pages, static assets, and bootstrap registration only."));
            }
        }
    }

    private static void ValidateSource(
        StorefrontVisualConsumerProfile profile,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        foreach (var file in EnumerateSourceFiles(profile.AbsoluteRoot))
        {
            var relativePath = ToProfileRelativePath(profile, file);
            if (profile.AllowedSourceRelativePaths.Contains(relativePath, StringComparer.OrdinalIgnoreCase)
                || IsAllowedBootstrapFile(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in ForbiddenSourceTokens)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add(StorefrontVisualBoundaryViolation.Source(
                    relativePath,
                    token,
                    "Move transport, service location, endpoint routing, and application-service implementations to Storefront Presentation."));
            }

            if (IsBrowserScript(relativePath))
            {
                ValidateBrowserScriptTokens(relativePath, source, ForbiddenBrowserCommandTokens, "Application command invocation belongs in Storefront Presentation browser binders.", violations);
                ValidateBrowserScriptTokens(relativePath, source, ForbiddenBrowserPayloadTokens, "Command payload construction belongs in Storefront Presentation browser binders.", violations);
                ValidateBrowserScriptTokens(relativePath, source, ForbiddenBrowserBusinessTokens, "Business result interpretation belongs in Storefront Presentation browser binders or server-side services.", violations);
            }
        }
    }

    private static void ValidateBrowserScriptTokens(
        string relativePath,
        string source,
        IEnumerable<string> tokens,
        string remediation,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        foreach (var token in tokens)
        {
            if (!source.Contains(token, StringComparison.Ordinal))
            {
                continue;
            }

            violations.Add(StorefrontVisualBoundaryViolation.Source(
                relativePath,
                token,
                remediation));
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string absoluteRoot)
    {
        if (!Directory.Exists(absoluteRoot))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(absoluteRoot, "*.*", SearchOption.AllDirectories))
        {
            if (SourceExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
                && !IsBuildOutput(path))
            {
                yield return path;
            }
        }
    }

    private static bool IsAllowedBootstrapFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        return fileName.Equals("Program.cs", StringComparison.Ordinal)
            || fileName.EndsWith("FoundationViewRegistration.cs", StringComparison.Ordinal)
            || fileName.Equals("appsettings.json", StringComparison.Ordinal)
            || fileName.StartsWith("appsettings.", StringComparison.Ordinal);
    }

    private static bool IsBrowserScript(string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        return extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mjs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ts", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToProfileRelativePath(StorefrontVisualConsumerProfile profile, string absolutePath)
    {
        return NormalizePath(Path.GetRelativePath(profile.AbsoluteRoot, absolutePath));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

internal sealed record StorefrontVisualConsumerProfile(
    string Name,
    string AbsoluteRoot,
    string RelativeProjectPath,
    IReadOnlyCollection<string> AllowedProjectReferenceFragments,
    IReadOnlyCollection<string> AllowedPackageReferences,
    IReadOnlyCollection<string> AllowedSourceRelativePaths)
{
    public string AbsoluteProjectPath => Path.Combine(AbsoluteRoot, RelativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
}

internal sealed record StorefrontVisualBoundaryViolation(
    string RelativePath,
    string Forbidden,
    string Owner,
    string Remediation)
{
    public static StorefrontVisualBoundaryViolation Project(string relativePath, string forbidden, string remediation)
    {
        return new(relativePath, forbidden, "Storefront Presentation package boundary", remediation);
    }

    public static StorefrontVisualBoundaryViolation Source(string relativePath, string forbidden, string remediation)
    {
        return new(relativePath, forbidden, "BlazorShop.Storefront.Presentation", remediation);
    }

    public override string ToString()
    {
        return $"{RelativePath}: forbidden '{Forbidden}'. Owner: {Owner}. Remediation: {Remediation}";
    }
}
