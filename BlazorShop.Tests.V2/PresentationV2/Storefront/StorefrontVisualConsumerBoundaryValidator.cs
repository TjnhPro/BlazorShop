namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Text.RegularExpressions;
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

    private static readonly string[] ForbiddenSourceTokens =
    [
        "HttpClient",
        "IHttpClientFactory",
        "StorefrontLocalApiClient",
        "fetch(",
        "XMLHttpRequest",
        "/api/storefront/",
        "/api/cart",
        "/api/checkout",
        "/api/consent",
        "/api/product-selection-preview",
        "blazorshop-antiforgery-token",
        "blazorshop-antiforgery-header",
        "IServiceProvider",
        "[Inject] IServiceProvider",
        "GetAsync<",
        "PostJsonAsync<",
        "PutJsonAsync<",
        "DeleteAsync<",
        "GetRequiredService<",
        "GetRequiredService(",
        "GetService<",
        "GetService(",
        "IdempotencyKey",
        "ExpectedCartVersion",
        "ExpectedCheckoutVersion",
        ": IStorefront",
        ": StorefrontRuntime",
        "StorefrontApiClient",
        "ManualStorefront",
    ];

    private static readonly BrowserForbiddenPattern[] ForbiddenSourcePatterns =
    [
        new(
            "StorefrontBrowser*Request",
            new Regex(@"\bStorefrontBrowser[A-Za-z0-9_]*Request\b", RegexOptions.CultureInvariant)),
    ];

    private static readonly string[] ForbiddenBootstrapTokens =
    [
        "AddHttpClient",
        "AddScoped<",
        "AddScoped(",
        "AddSingleton<",
        "AddSingleton(",
        "AddTransient<",
        "AddTransient(",
        "MapGet(",
        "MapPost(",
        "MapPut(",
        "MapDelete(",
        "MapMethods(",
        "MapGroup(",
        "UseMiddleware",
        "UseWhen(",
        "AddStorefrontRuntime",
        "AddStorefrontPlatformRuntime",
        "AddStorefrontPresentation(",
        "UseStorefrontPresentation(",
        "MapStorefrontPresentation(",
        "MapRazorComponents<",
    ];

    private static readonly string[] ForbiddenWasmProgramTokens =
    [
        "AddStorefrontBrowserCart",
        "AddStorefrontBrowserCheckout",
        "AddStorefrontBrowserAccount",
    ];

    private static readonly string[] ForbiddenServerBrowserProgramTokens =
    [
        "AddStorefrontBrowserCart",
        "AddStorefrontBrowserCheckout",
        "AddStorefrontBrowserAccount",
        "AddStorefrontBrowserRuntime",
    ];

    private static readonly string[] ForbiddenBrowserCommandTokens =
    [
        ".application.cart.",
        ".application.consent.",
        ".application.productSelection.",
        "blazorShopStorefront.application",
        "blazorShopStorefront.bindings.addToCart",
        "blazorShopStorefront.bindings.productSelection",
        "application.cart",
        "application.consent",
        "application.productSelection",
        "bindings.addToCart",
        "bindings.productSelection",
        "cart.addLine",
        "cart.updateLine",
        "cart.removeLine",
        "cart.clear",
        "cart.recalculate",
        "productSelection.preview",
        "addPurchaseLine(",
        "previewPurchase(",
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

    private static readonly BrowserForbiddenPattern[] ForbiddenBrowserBusinessPatterns =
    [
        RawPreviewPattern("canAddToCart"),
        RawPreviewPattern("stockQuantity"),
        RawPreviewPattern("isAvailable"),
        RawPreviewPattern("validationMessages"),
        RawPreviewPattern("unitPrice"),
        RawPreviewPattern("formattedUnitPrice"),
        RawPreviewPattern("formattedComparePrice"),
        RawPreviewPattern("sku"),
        RawPreviewPattern("gtin"),
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

            violations.Add(StorefrontVisualBoundaryViolation.Project(
                profile.RelativeProjectPath,
                $"ProjectReference:{reference}",
                "Visual consumer project references must be explicitly allowlisted by profile; move runtime/client/backend logic behind Presentation or Browser."));
        }

        foreach (var package in document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (profile.AllowedPackageReferences.Contains(package, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            violations.Add(StorefrontVisualBoundaryViolation.Project(
                profile.RelativeProjectPath,
                $"PackageReference:{package}",
                "Visual consumer package references must be explicitly allowlisted by profile. Runtime/Client package metadata belongs outside visual projects."));
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
                || IsAppSettingsFile(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            ValidateSourceTokens(
                relativePath,
                source,
                ForbiddenSourceTokens,
                "Move transport, service location, endpoint routing, and application-service implementations to Storefront Presentation.",
                violations);
            ValidateSourcePatterns(relativePath, source, ForbiddenSourcePatterns, "Move Browser request DTO construction into BlazorShop.Storefront.Browser controllers.", violations);
            ValidateBootstrapSource(profile, relativePath, source, violations);

            if (IsBrowserScript(relativePath))
            {
                ValidateBrowserScriptTokens(relativePath, source, ForbiddenBrowserCommandTokens, "Application command invocation belongs in Storefront Presentation browser binders.", violations);
                ValidateBrowserScriptTokens(relativePath, source, ForbiddenBrowserPayloadTokens, "Command payload construction belongs in Storefront Presentation browser binders.", violations);
                ValidateBrowserScriptPatterns(relativePath, source, ForbiddenBrowserBusinessPatterns, "Business result interpretation belongs in Storefront Presentation browser binders or server-side services.", violations);
            }
        }
    }

    private static void ValidateBootstrapSource(
        StorefrontVisualConsumerProfile profile,
        string relativePath,
        string source,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        var fileName = Path.GetFileName(relativePath);
        if (!fileName.Equals("Program.cs", StringComparison.Ordinal)
            && !fileName.EndsWith("FoundationViewRegistration.cs", StringComparison.Ordinal))
        {
            return;
        }

        ValidateSourceTokens(
            relativePath,
            source,
            ForbiddenBootstrapTokens,
            "Bootstrap files may compose the Storefront application and view registrations only; move service registration, middleware, endpoint mapping, and transport setup to Storefront Presentation or Browser runtime.",
            violations);

        if (!fileName.Equals("Program.cs", StringComparison.Ordinal))
        {
            return;
        }

        if (!profile.Name.Contains("WASM", StringComparison.OrdinalIgnoreCase))
        {
            ValidateSourceTokens(
                relativePath,
                source,
                ForbiddenServerBrowserProgramTokens,
                "Server visual hosts must use AddStorefrontBrowserControllers for prerender-safe controller registration and must not register WASM runtime transport or individual Browser capabilities.",
                violations);

            return;
        }

        ValidateSourceTokens(
            relativePath,
            source,
            ForbiddenWasmProgramTokens,
            "V2.WASM Program.cs must call AddStorefrontBrowserRuntime only; Browser runtime owns feature service registration.",
            violations);

        if (!source.Contains("WebAssemblyHostBuilder.CreateDefault(args)", StringComparison.Ordinal))
        {
            violations.Add(StorefrontVisualBoundaryViolation.Source(
                relativePath,
                "missing WebAssemblyHostBuilder.CreateDefault(args)",
                "V2.WASM bootstrap should only create the WASM host builder and delegate runtime registration to Browser."));
        }

        if (!source.Contains("AddStorefrontBrowserRuntime(builder.HostEnvironment)", StringComparison.Ordinal))
        {
            violations.Add(StorefrontVisualBoundaryViolation.Source(
                relativePath,
                "missing AddStorefrontBrowserRuntime(builder.HostEnvironment)",
                "V2.WASM bootstrap must use the Browser runtime registration extension instead of registering transport/application services directly."));
        }
    }

    private static BrowserForbiddenPattern RawPreviewPattern(string fieldName)
    {
        var escaped = Regex.Escape(fieldName);
        return new BrowserForbiddenPattern(
            $"preview.{fieldName}",
            new Regex($@"\bpreview\s*(?:\.\s*{escaped}|\[\s*[""']{escaped}[""']\s*\])", RegexOptions.CultureInvariant));
    }

    private static void ValidateBrowserScriptTokens(
        string relativePath,
        string source,
        IEnumerable<string> tokens,
        string remediation,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        ValidateSourceTokens(relativePath, source, tokens, remediation, violations);
    }

    private static void ValidateSourceTokens(
        string relativePath,
        string source,
        IEnumerable<string> tokens,
        string remediation,
        List<StorefrontVisualBoundaryViolation> violations,
        Func<string, bool>? isAllowed = null)
    {
        foreach (var token in tokens)
        {
            if (!source.Contains(token, StringComparison.Ordinal))
            {
                continue;
            }

            if (isAllowed?.Invoke(token) == true)
            {
                continue;
            }

            violations.Add(StorefrontVisualBoundaryViolation.Source(
                relativePath,
                token,
                remediation));
        }
    }

    private static void ValidateSourcePatterns(
        string relativePath,
        string source,
        IEnumerable<BrowserForbiddenPattern> patterns,
        string remediation,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        foreach (var pattern in patterns)
        {
            if (!pattern.Pattern.IsMatch(source))
            {
                continue;
            }

            violations.Add(StorefrontVisualBoundaryViolation.Source(
                relativePath,
                pattern.Forbidden,
                remediation));
        }
    }

    private static void ValidateBrowserScriptPatterns(
        string relativePath,
        string source,
        IEnumerable<BrowserForbiddenPattern> patterns,
        string remediation,
        List<StorefrontVisualBoundaryViolation> violations)
    {
        foreach (var pattern in patterns)
        {
            if (!pattern.Pattern.IsMatch(source))
            {
                continue;
            }

            violations.Add(StorefrontVisualBoundaryViolation.Source(
                relativePath,
                pattern.Forbidden,
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

    private static bool IsAppSettingsFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        return fileName.Equals("appsettings.json", StringComparison.Ordinal)
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

internal sealed record BrowserForbiddenPattern(string Forbidden, Regex Pattern);

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
