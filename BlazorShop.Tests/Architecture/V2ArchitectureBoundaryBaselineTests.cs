namespace BlazorShop.Tests.Architecture
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed class V2ArchitectureBoundaryBaselineTests
    {
        [Fact]
        public void ControlPlaneCommerceCatalogResult_ReferenceCountMatchesCurrentMigrationBaseline()
        {
            var references = EnumerateSourceFiles(
                    "BlazorShop.Application",
                    "BlazorShop.Infrastructure",
                    "BlazorShop.PresentationV2")
                .SelectMany(path => Regex.Matches(File.ReadAllText(path), "ControlPlaneCommerceCatalogResult<"))
                .Count();

            Assert.InRange(references, 110, 125);
        }

        [Fact]
        public void ApplicationGatewayContracts_DoNotExposeHttpTransportPrimitives()
        {
            var offenders = EnumerateSourceFiles("BlazorShop.Application/ControlPlane/CommerceGateway")
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return source.Contains("HttpMethod", StringComparison.Ordinal)
                           || source.Contains("HttpStatusCode", StringComparison.Ordinal)
                           || source.Contains("HttpClient", StringComparison.Ordinal);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
            Assert.False(File.Exists(RepositoryPath("BlazorShop.Application/ControlPlane/CommerceGateway/CommerceNodeAdminGatewayDtos.cs")));
        }

        [Fact]
        public void ApplicationResultModel_ExistsBeforeGatewayMigration()
        {
            var result = ReadRepositoryFile("BlazorShop.Application/Common/Results/ApplicationResult.cs");
            var error = ReadRepositoryFile("BlazorShop.Application/Common/Results/ApplicationError.cs");
            var errorKind = ReadRepositoryFile("BlazorShop.Application/Common/Results/ApplicationErrorKind.cs");

            Assert.Contains("record ApplicationResult<TValue>", result, StringComparison.Ordinal);
            Assert.Contains("record ApplicationError", error, StringComparison.Ordinal);
            Assert.Contains("ApplicationError RemoteFailure", error, StringComparison.Ordinal);
            Assert.Contains("RemoteFailure", errorKind, StringComparison.Ordinal);
        }

        [Fact]
        public void ControlPlaneProductGateway_BaselineStillContainsMixedCapabilities()
        {
            var source = ReadRepositoryFile("BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductGateway.cs");
            var methodCount = Regex.Matches(source, "Task<ControlPlaneCommerceCatalogResult").Count;

            Assert.Equal(31, methodCount);
            Assert.Contains("GetProductSeoAsync", source, StringComparison.Ordinal);
            Assert.Contains("UploadProductImportAsync", source, StringComparison.Ordinal);
            Assert.Contains("ListVariationTemplatesAsync", source, StringComparison.Ordinal);
            Assert.Contains("SetCategoryPrimaryMediaAsync", source, StringComparison.Ordinal);
            Assert.Contains("QueryInventoryAsync", source, StringComparison.Ordinal);
        }

        [Fact]
        public void NullableProductionDependencyInventory_IsExplicitlyAllowlistedAtPhase0()
        {
            var offenderPatterns = new[]
            {
                "IProductSelectionResolver?",
                "new ProductSelectionResolver",
                "IOptions<StorefrontCartOptions>?",
                "IStorefrontNavigationCache?",
                "IStoreSeoSlugPolicyService?",
                "IStoreSeoSlugHistoryService?",
                "ISeoRedirectAutomationService?",
                "IStorefrontStoreConfigurationClient?",
                "IHttpContextAccessor?",
                "new HttpContextAccessor",
                "ICatalogQueryCache?",
                "IStoreShippingSettingsService?",
                "ICommerceTransactionalMessageService?",
            };

            var offenders = EnumerateSourceFiles(
                    "BlazorShop.Application",
                    "BlazorShop.Infrastructure",
                    "BlazorShop.PresentationV2")
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return offenderPatterns.Any(pattern => source.Contains(pattern, StringComparison.Ordinal));
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "BlazorShop.Application/CommerceNode/Carts/StorefrontCartService.cs",
                    "BlazorShop.Application/Services/CategorySeoService.cs",
                    "BlazorShop.Application/Services/CategoryService.cs",
                    "BlazorShop.Application/Services/ProductSeoService.cs",
                    "BlazorShop.Application/Services/ProductService.cs",
                    "BlazorShop.Application/Services/ProductVariantService.cs",
                    "BlazorShop.Application/Services/PublicCatalogService.cs",
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeAdminShipmentService.cs",
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeOrderTrackingService.cs",
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/ShippingProviders.cs",
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCartSessionService.cs",
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontPageService.cs",
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/VariationTemplateService.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontDisplayContextProvider.cs",
                ],
                offenders);
        }

        [Fact]
        public void StorefrontConcreteApiClientUsage_IsExplicitlyAllowlistedAtPhase0()
        {
            var concreteUsages = EnumerateSourceFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2")
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return source.Contains("StorefrontApiClient ", StringComparison.Ordinal)
                           || source.Contains("@inject StorefrontApiClient", StringComparison.Ordinal);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAccountEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAuthFormEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCartEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCheckoutEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontConsentEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountAddressesPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrderDetailPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrdersPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountProfilePage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CategoryPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CheckoutPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Home.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/NewReleases.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/PaymentCancelPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/PaymentSuccessPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/SearchPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/StorefrontPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/TodaysDeals.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs",
                ],
                concreteUsages);
        }

        [Fact]
        public void WebSharedV2BusinessModelFolders_AreExplicitlyAllowlistedAtPhase0()
        {
            var modelRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Models");
            var folders = Directory.EnumerateDirectories(modelRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "Authentication",
                    "Category",
                    "Discovery",
                    "Pages",
                    "Payment",
                    "Product",
                    "Seo",
                ],
                folders);
        }

        [Fact]
        public void StorefrontScopedResolveStoreIdDuplication_IsExplicitlyAllowlistedAtPhase0()
        {
            var controllers = EnumerateSourceFiles("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront")
                .Where(path => File.ReadAllText(path).Contains("ResolveStoreIdAsync", StringComparison.Ordinal))
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedAddressController.cs",
                    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCartController.cs",
                    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCheckoutController.cs",
                    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedConsentController.cs",
                    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCustomerAddressesController.cs",
                    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedPaymentsController.cs",
                ],
                controllers);
        }

        [Fact]
        public void InfrastructureStoreContext_StillReadsAmbientHttpContextAtPhase0()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceStoreContext.cs");

            Assert.Contains("IHttpContextAccessor", source, StringComparison.Ordinal);
            Assert.Contains("request.RouteValues[\"storeKey\"]", source, StringComparison.Ordinal);
        }

        [Fact]
        public void KnownHotspotFileSizes_MatchPhase0Baseline()
        {
            var hotspots = new[]
            {
                new HotspotBaseline("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs", 1675),
                new HotspotBaseline("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.cs", 1787),
                new HotspotBaseline("BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneDbContext.cs", 712, 27),
                new HotspotBaseline("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor", 1691),
                new HotspotBaseline("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.cs", 813),
            };

            foreach (var hotspot in hotspots)
            {
                var path = RepositoryPath(hotspot.RelativePath);
                var source = File.ReadAllText(path);
                Assert.Equal(hotspot.LineCount, File.ReadLines(path).Count());

                if (hotspot.ModelBuilderEntityCount is not null)
                {
                    Assert.Equal(
                        hotspot.ModelBuilderEntityCount,
                        Regex.Matches(source, "modelBuilder\\.Entity").Count);
                }
            }
        }

        [Fact]
        public void ActiveV2TestSurface_IsNotYetIsolatedFromLegacyAtPhase0()
        {
            var testProject = ReadRepositoryFile("BlazorShop.Tests/BlazorShop.Tests.csproj");

            Assert.Contains("BlazorShop.Presentation\\BlazorShop.API", testProject, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2\\BlazorShop.ControlPlane.API", testProject, StringComparison.Ordinal);
            Assert.False(File.Exists(RepositoryPath("BlazorShop.V2.slnf")));
        }

        private static IEnumerable<string> EnumerateSourceFiles(params string[] relativeRoots)
        {
            var excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                "node_modules",
            };

            foreach (var relativeRoot in relativeRoots)
            {
                var root = RepositoryPath(relativeRoot);
                foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    var extension = Path.GetExtension(path);
                    if (path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Any(segment => excludedDirectories.Contains(segment)))
                    {
                        continue;
                    }

                    if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".razor", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return path;
                    }
                }
            }
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot().FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToRepositoryRelativePath(string path)
        {
            return Path.GetRelativePath(FindRepositoryRoot().FullName, path)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
            {
                current = current.Parent;
            }

            Assert.NotNull(current);
            return current!;
        }
    }

    internal sealed record HotspotBaseline(
        string RelativePath,
        int LineCount,
        int? ModelBuilderEntityCount = null);
}
