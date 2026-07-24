namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed class StorefrontBffBoundaryHardeningTests
    {
        [Fact]
        public void LocalEndpointGroups_StayOwnedByStorefrontV2Bff()
        {
            var endpointSources = ReadEndpointSources();

            Assert.Contains("app.MapGet(\"/api/cart\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/product-selection-preview\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/account/profile\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/account/change-password\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/checkout\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/checkout/place-order\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/api/consent/current\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapPost(\"/api/consent/revoke\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(StorefrontRoutes.Robots", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(StorefrontRoutes.Sitemap", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/media/products/{mediaPublicId:guid}\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/media/assets/{assetPublicId:guid}/{fileName}\"", endpointSources, StringComparison.Ordinal);
        }

        [Fact]
        public void LocalEndpointContracts_AreSplitIntoCapabilitySpecificContractFiles()
        {
            var endpointDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints");
            var supportSources = Directory.EnumerateFiles(endpointDirectory, "StorefrontLocalEndpointSupport*.cs", SearchOption.TopDirectoryOnly)
                .Select(File.ReadAllText)
                .ToArray();
            var supportSource = string.Join(Environment.NewLine, supportSources);
            var publicEndpointTypeDeclaration = new Regex(
                @"^\s*public\s+(?:sealed\s+)?(?:class|record)\s+Storefront(?:Local|Currency)",
                RegexOptions.Multiline);

            Assert.DoesNotContain("StorefrontLocalCartLineRequest", supportSource, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalProductSelectionPreviewRequest", supportSource, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalCartQuantityRequest", supportSource, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontCurrencyPreferenceForm", supportSource, StringComparison.Ordinal);
            Assert.DoesNotContain(supportSources, source => publicEndpointTypeDeclaration.IsMatch(source));

            var contractSources = Directory.EnumerateFiles(Path.Combine(endpointDirectory, "Contracts"), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(File.ReadAllText)
                .ToArray();
            var contractSource = string.Join(Environment.NewLine, contractSources);

            Assert.Contains("StorefrontLocalCartLineRequest", contractSource, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalProductSelectionPreviewRequest", contractSource, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalProductSelectionPreviewResponse", contractSource, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalCartQuantityRequest", contractSource, StringComparison.Ordinal);
            Assert.Contains("StorefrontCurrencyPreferenceForm", contractSource, StringComparison.Ordinal);
        }

        [Fact]
        public void LocalEndpointErrors_UseCentralBrowserSafeMapping()
        {
            var support = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.cs");
            var endpointSources = ReadEndpointSources();

            Assert.Contains("LocalSignInRequired", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status401Unauthorized", support, StringComparison.Ordinal);
            Assert.Contains("LocalForbidden", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status403Forbidden", support, StringComparison.Ordinal);
            Assert.Contains("LocalConflict", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status409Conflict", support, StringComparison.Ordinal);
            Assert.Contains("LocalUnprocessable", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status422UnprocessableEntity", support, StringComparison.Ordinal);
            Assert.Contains("LocalServerError", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status500InternalServerError", support, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalApiErrorResponse", support, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalCartErrorResponse", support, StringComparison.Ordinal);
            Assert.Contains("NormalizeLocalErrorMessage", support, StringComparison.Ordinal);
            Assert.Contains("LocalApiValidationError", endpointSources, StringComparison.Ordinal);
            Assert.Contains("LocalCartValidationError", endpointSources, StringComparison.Ordinal);
            Assert.Contains("LocalConflict", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs"), StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest(new StorefrontLocal", endpointSources, StringComparison.Ordinal);
        }

        [Fact]
        public void BrowserProjects_DoNotKnowCommerceNodeOrProtectedTokens()
        {
            var browserRoots = new[]
            {
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components"),
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.WASM"),
            };
            var bannedTerms = new[]
            {
                "api/storefront/stores",
                "CommerceNode",
                "NodeSecret",
                "NodeKey",
                "accessToken",
                "refreshToken",
            };

            var offenders = browserRoots
                .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    .Select(path => new
                    {
                        RelativePath = Path.GetRelativePath(RepositoryRoot(), path),
                        Source = File.ReadAllText(path),
                    }))
                .SelectMany(file => bannedTerms
                    .Where(term => file.Source.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(term => $"{file.RelativePath}: {term}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
        }

        [Fact]
        public void BrowserBffBoundary_IsDocumentedAsStorefrontV2Responsibility()
        {
            var architecture = ReadRepositoryFile("docs/architecture/03-runtime-boundaries.md");

            Assert.Contains("### Browser/BFF Boundary", architecture, StringComparison.Ordinal);
            Assert.Contains("resolving the current store", architecture, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("HttpOnly customer session", architecture, StringComparison.Ordinal);
            Assert.Contains("Commerce access tokens server-side", architecture, StringComparison.Ordinal);
            Assert.Contains("cart token", architecture, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("antiforgery", architecture, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("normalizing Commerce API failures", architecture, StringComparison.Ordinal);
            Assert.Contains("local/browser-safe response shapes", architecture, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("price calculation", architecture, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sellability calculation", architecture, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("order creation outside Commerce checkout/place-order APIs", architecture, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadEndpointSources()
        {
            var endpointDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints");
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(endpointDirectory, "*.cs", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
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
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate BlazorShop.sln.");
        }
    }
}
