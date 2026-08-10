namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed class StorefrontBffBoundaryHardeningTests
    {
        [Fact]
        public void LocalEndpointGroups_StayOwnedByStorefrontHostBff()
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
            Assert.Contains("endpoints.MapGet(StorefrontRoutes.Robots", endpointSources, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapGet(StorefrontRoutes.Sitemap", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/media/products/{mediaPublicId:guid}\"", endpointSources, StringComparison.Ordinal);
            Assert.Contains("app.MapGet(\"/media/assets/{assetPublicId:guid}/{fileName}\"", endpointSources, StringComparison.Ordinal);
        }

        [Fact]
        public void LocalEndpointRouteInventory_RecordsCurrentBrowserContracts()
        {
            var endpointSources = ReadEndpointSources();
            var expectedMappings = new[]
            {
                "app.MapGet(\"/api/cart\"",
                "app.MapPost(\"/api/product-selection-preview\"",
                "app.MapPost(\"/api/cart/lines\"",
                "app.MapPut(\"/api/cart/lines/{lineId:guid}\"",
                "app.MapDelete(\"/api/cart/lines/{lineId:guid}\"",
                "app.MapDelete(\"/api/cart\"",
                "app.MapPost(\"/api/cart/recalculate\"",
                "app.MapGet(\"/api/checkout\"",
                "app.MapPost(\"/api/checkout/addresses\"",
                "app.MapPost(\"/api/checkout/shipping-method\"",
                "app.MapPost(\"/api/checkout/payment-method\"",
                "app.MapPost(\"/api/checkout/review\"",
                "app.MapPost(\"/api/checkout/place-order\"",
                "app.MapGet(\"/api/account/profile\"",
                "app.MapGet(\"/api/account/addresses\"",
                "app.MapPost(\"/api/account/addresses\"",
                "app.MapPost(\"/api/account/addresses/{addressId:guid}/default-shipping\"",
                "app.MapPost(\"/api/account/addresses/{addressId:guid}/default-billing\"",
                "app.MapGet(\"/api/account/orders\"",
                "app.MapGet(\"/api/account/orders/{orderReference}\"",
                "app.MapGet(\"/api/account/orders/{orderReference}/receipt\"",
                "app.MapPost(\"/api/account/change-password\"",
                "app.MapGet(\"/api/consent/current\"",
                "app.MapPost(\"/api/consent\"",
                "app.MapPost(\"/api/consent/revoke\"",
                "endpoints.MapGet(StorefrontRoutes.Robots",
                "endpoints.MapGet(StorefrontRoutes.Sitemap",
                "app.MapGet(\"/media/products/{mediaPublicId:guid}\"",
                "app.MapGet(\"/media/assets/{assetPublicId:guid}/{fileName}\"",
            };

            foreach (var expectedMapping in expectedMappings)
            {
                Assert.Contains(expectedMapping, endpointSources, StringComparison.Ordinal);
            }

            Assert.Contains("StorefrontLocalApiErrorResponse", endpointSources, StringComparison.Ordinal);
            Assert.Contains("StorefrontBrowserCart", ReadCartPresentationSources(), StringComparison.Ordinal);
            Assert.Contains("StorefrontBrowserCheckoutState", endpointSources, StringComparison.Ordinal);
            Assert.Contains("StorefrontBrowserCustomerProfile", endpointSources, StringComparison.Ordinal);
        }

        [Fact]
        public void LocalEndpointContracts_AreSplitIntoCapabilitySpecificContractFiles()
        {
            var presentationEndpointDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints");
            var supportSources = Directory.EnumerateFiles(presentationEndpointDirectory, "StorefrontLocalEndpointSupport*.cs", SearchOption.TopDirectoryOnly)
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

            var contractSources = Directory.EnumerateFiles(Path.Combine(presentationEndpointDirectory, "Contracts"), "*.cs", SearchOption.TopDirectoryOnly)
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
            var support = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints"),
                        "StorefrontLocalEndpointSupport*.cs",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            var endpointSources = ReadEndpointSources();

            Assert.Contains("LocalSignInRequired", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status401Unauthorized", support, StringComparison.Ordinal);
            Assert.Contains("LocalConflict", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status409Conflict", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status422UnprocessableEntity", support, StringComparison.Ordinal);
            Assert.Contains("LocalUnavailable", support, StringComparison.Ordinal);
            Assert.Contains("StatusCodes.Status503ServiceUnavailable", support, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalApiErrorResponse", support, StringComparison.Ordinal);
            Assert.Contains("StorefrontLocalCartErrorResponse", support, StringComparison.Ordinal);
            Assert.Contains("NormalizeLocalErrorMessage", support, StringComparison.Ordinal);
            Assert.Contains("DefaultLocalErrorCode", support, StringComparison.Ordinal);
            Assert.Contains("CurrentTraceId", support, StringComparison.Ordinal);
            Assert.Contains("NormalizeFieldErrors", support, StringComparison.Ordinal);
            Assert.Contains("Retryable", support, StringComparison.Ordinal);
            Assert.Contains("StatusCode", support, StringComparison.Ordinal);
            Assert.Contains("LocalUnavailable", endpointSources, StringComparison.Ordinal);
            Assert.Contains("LocalNotFound", endpointSources, StringComparison.Ordinal);
            Assert.Contains("LocalApiValidationError", endpointSources, StringComparison.Ordinal);
            Assert.Contains("LocalCartValidationError", endpointSources, StringComparison.Ordinal);
            Assert.Contains("LocalConflict", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs"), StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest(new StorefrontLocal", endpointSources, StringComparison.Ordinal);
        }

        [Fact]
        public void BrowserProjects_DoNotKnowCommerceNodeOrProtectedTokens()
        {
            var browserRoots = new[]
            {
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components"),
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Browser"),
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM"),
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
                    .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
                            || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)))
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
            var endpointDirectories = new[]
            {
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints"),
            };
            return string.Join(
                Environment.NewLine,
                endpointDirectories.SelectMany(endpointDirectory => Directory.EnumerateFiles(endpointDirectory, "*.cs", SearchOption.AllDirectories))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private static string ReadCartPresentationSources()
        {
            var cartRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Cart");
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(cartRoot, "*.cs", SearchOption.TopDirectoryOnly)
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
