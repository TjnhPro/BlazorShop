namespace BlazorShop.Tests.Architecture
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed class V2ArchitectureBoundaryBaselineTests
    {
        [Fact]
        public void ControlPlaneCommerceCatalogResult_IsAbsentFromActiveCode()
        {
            var references = EnumerateSourceFiles(
                    "BlazorShop.Application",
                    "BlazorShop.Infrastructure",
                    "BlazorShop.PresentationV2")
                .SelectMany(path => Regex.Matches(File.ReadAllText(path), "ControlPlaneCommerceCatalogResult"))
                .Count();

            Assert.Equal(0, references);
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
                           || source.Contains("HttpClient", StringComparison.Ordinal)
                           || source.Contains("HttpRequestMessage", StringComparison.Ordinal)
                           || source.Contains("HttpResponseMessage", StringComparison.Ordinal)
                           || source.Contains("ServiceResponse<", StringComparison.Ordinal)
                           || Regex.IsMatch(source, @"\bstring\??\s+(path|relativePath|url|endpoint|route)\b", RegexOptions.IgnoreCase);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
            Assert.False(File.Exists(RepositoryPath("BlazorShop.Application/ControlPlane/CommerceGateway/CommerceNodeAdminGatewayDtos.cs")));
        }

        [Fact]
        public void ControlPlaneCommerceGatewayInterfaces_UseApplicationResultCapabilities()
        {
            var gatewayInterfaces = EnumerateSourceFiles("BlazorShop.Application/ControlPlane/CommerceGateway")
                .Where(path => Path.GetFileName(path).StartsWith("IControlPlane", StringComparison.Ordinal))
                .Select(path => new
                {
                    Path = path,
                    Source = File.ReadAllText(path),
                })
                .ToArray();

            Assert.NotEmpty(gatewayInterfaces);

            var nonApplicationResultTasks = gatewayInterfaces
                .Where(file => Regex.Matches(file.Source, @"Task<").Count
                               != Regex.Matches(file.Source, @"Task<ApplicationResult<").Count)
                .Select(file => ToRepositoryRelativePath(file.Path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var oversizedCapabilities = gatewayInterfaces
                .Select(file => new
                {
                    RelativePath = ToRepositoryRelativePath(file.Path),
                    MethodCount = Regex.Matches(file.Source, @"Task<ApplicationResult<").Count,
                })
                .Where(file => file.MethodCount is < 1 or > 15)
                .Select(file => $"{file.RelativePath}: {file.MethodCount}")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(nonApplicationResultTasks);
            Assert.Empty(oversizedCapabilities);
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
        public void ControlPlaneProductGateway_IsSplitByCapability()
        {
            var source = ReadRepositoryFile("BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductGateway.cs");
            var methodCount = Regex.Matches(source, "Task<ApplicationResult").Count;

            Assert.Equal(9, methodCount);
            Assert.Contains("ListVariantsAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("GetProductSeoAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("UploadProductImportAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ListVariationTemplatesAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SetCategoryPrimaryMediaAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("QueryInventoryAsync", source, StringComparison.Ordinal);

            Assert.Contains(
                "interface IControlPlaneProductSeoGateway",
                ReadRepositoryFile("BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductSeoGateway.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "interface IControlPlaneProductImportGateway",
                ReadRepositoryFile("BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductImportGateway.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "interface IControlPlaneVariationTemplateGateway",
                ReadRepositoryFile("BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneVariationTemplateGateway.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "interface IControlPlaneInventoryGateway",
                ReadRepositoryFile("BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneInventoryGateway.cs"),
                StringComparison.Ordinal);

            var capabilityInterfaces = new[]
            {
                "BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductGateway.cs",
                "BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductSeoGateway.cs",
                "BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneProductImportGateway.cs",
                "BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneVariationTemplateGateway.cs",
                "BlazorShop.Application/ControlPlane/CommerceGateway/Products/IControlPlaneInventoryGateway.cs",
            };

            foreach (var capabilityInterface in capabilityInterfaces)
            {
                var capabilitySource = ReadRepositoryFile(capabilityInterface);
                Assert.InRange(Regex.Matches(capabilitySource, "Task<ApplicationResult").Count, 1, 15);
            }
        }

        [Fact]
        public void ActiveV2ProductionConstructors_DoNotUseNullableDependencyFallbacks()
        {
            var offenders = EnumerateSourceFiles(
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services",
                    "BlazorShop.Infrastructure/Data/ControlPlane",
                    "BlazorShop.PresentationV2")
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return Regex.IsMatch(
                               source,
                               @"public\s+[A-Za-z0-9_]+\s*\([^)]*I[A-Za-z0-9_<>,\s]+\?\s+\w+\s*=\s*null",
                               RegexOptions.Singleline)
                           || source.Contains("new ProductSelectionResolver", StringComparison.Ordinal)
                           || source.Contains("new HttpContextAccessor", StringComparison.Ordinal)
                           || source.Contains("new StorefrontNavigationCache", StringComparison.Ordinal)
                           || source.Contains("new CatalogQueryCache", StringComparison.Ordinal);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
        }

        [Fact]
        public void StorefrontEndpointMappings_DoNotInjectConcreteApiClient()
        {
            var offenders = EnumerateSourceFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints")
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return source.Contains("StorefrontApiClient ", StringComparison.Ordinal)
                           || source.Contains("@inject StorefrontApiClient", StringComparison.Ordinal);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
        }

        [Fact]
        public void StorefrontConcreteApiClientUsage_IsEliminatedInActivePresentation()
        {
            var concreteUsages = EnumerateSourceFiles(
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components")
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return source.Contains("StorefrontApiClient ", StringComparison.Ordinal)
                           || source.Contains("@inject StorefrontApiClient", StringComparison.Ordinal);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(concreteUsages);
        }

        [Fact]
        public void WebSharedV2BusinessModelFolders_AreFrozenDuringContractMigration()
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
        public void StorefrontScopedResolveStoreIdDuplication_IsCentralizedAfterPhase5()
        {
            var controllers = EnumerateSourceFiles("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront")
                .Where(path => File.ReadAllText(path).Contains("private async Task<Guid?> ResolveStoreIdAsync", StringComparison.Ordinal))
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(controllers);
            Assert.Contains(
                "protected async Task<Guid?> ResolveStoreIdAsync",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontApiControllerBase.cs"),
                StringComparison.Ordinal);
        }

        [Fact]
        public void InfrastructureStoreContext_ReadsExecutionContextOnlyAfterPhase5()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceStoreContext.cs");

            Assert.Contains("IStoreExecutionContextAccessor", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IHttpContextAccessor", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.AspNetCore.Http", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RouteValues", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Request.Query", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Headers", source, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpRequest", source, StringComparison.Ordinal);
        }

        [Fact]
        public void InfrastructureServices_DoNotKeepPrivateStoreScopeResolutionHelpersAfterPhase5()
        {
            var offenders = EnumerateSourceFiles("BlazorShop.Infrastructure/Data/CommerceNode/Services")
                .Where(path => File.ReadAllText(path).Contains("private async Task<Guid?> ResolveStoreIdAsync", StringComparison.Ordinal))
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
        }

        [Fact]
        public void KnownHotspotFileSizes_MatchCurrentPhaseBaseline()
        {
            var hotspots = Array.Empty<HotspotBaseline>();

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
        public void StorefrontLocalEndpointSupport_IsSplitByEndpointConcernAfterPhase7F()
        {
            var supportFiles = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints"), "StorefrontLocalEndpointSupport*.cs")
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Account.cs", supportFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Cart.cs", supportFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs", supportFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.cs", supportFiles);
            Assert.All(supportFiles, file => Assert.True(File.ReadLines(RepositoryPath(file)).Count() <= 320));
            Assert.All(supportFiles, file => Assert.Contains("internal static partial class StorefrontLocalEndpointSupport", ReadRepositoryFile(file), StringComparison.Ordinal));
        }

        [Fact]
        public void CommerceProductsPage_IsSplitIntoMarkupAndCodeBehindAfterPhase7D()
        {
            var markupPath = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor");
            var codeBehindPath = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor.cs");
            var componentDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/CommerceProducts");
            var markup = File.ReadAllText(markupPath);
            var codeBehind = File.ReadAllText(codeBehindPath);
            var componentFiles = Directory
                .EnumerateFiles(componentDirectory, "*.razor")
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);
            Assert.Contains("public partial class CommerceProducts", codeBehind, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/CommerceProducts/CommerceProductBasicInfoPanel.razor", componentFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/CommerceProducts/CommerceProductInventoryPanel.razor", componentFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/CommerceProducts/CommerceProductMediaPanel.razor", componentFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/CommerceProducts/CommerceProductSeoPanel.razor", componentFiles);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/CommerceProducts/CommerceProductVariationsPanel.razor", componentFiles);
            Assert.True(File.ReadLines(markupPath).Count() <= 300);
            Assert.True(File.ReadLines(codeBehindPath).Count() <= 1050);
            Assert.All(componentFiles, file => Assert.True(File.ReadLines(RepositoryPath(file)).Count() <= 240));
        }

        [Fact]
        public void LargeControlPlanePages_AreSplitIntoMarkupAndCodeBehindAfterPhase7E()
        {
            var pages = new[]
            {
                "Stores",
                "CommercePages",
                "CommerceEmailSettings",
                "CommerceCurrencies",
                "CommerceOrders",
                "CommerceCategories",
                "Users",
                "CommerceNavigation",
            };

            foreach (var page in pages)
            {
                var markupPath = RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/{page}.razor");
                var codeBehindPath = RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/{page}.razor.cs");
                var markup = File.ReadAllText(markupPath);
                var codeBehind = File.ReadAllText(codeBehindPath);

                Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);
                Assert.Contains($"public partial class {page}", codeBehind, StringComparison.Ordinal);
                Assert.True(File.ReadLines(markupPath).Count() <= 450);
                Assert.True(File.ReadLines(codeBehindPath).Count() <= 650);
            }
        }

        [Fact]
        public void ControlPlaneDbContext_UsesEntityTypeConfigurationsAfterPhase7C()
        {
            var dbContextSource = ReadRepositoryFile("BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneDbContext.cs");
            var configurationFiles = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.Infrastructure/Data/ControlPlane/Configurations"), "*.cs")
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Contains("ApplyConfigurationsFromAssembly(", dbContextSource);
            Assert.Contains("BlazorShop.Infrastructure.Data.ControlPlane.Configurations", dbContextSource);
            Assert.True(File.ReadLines(RepositoryPath("BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneDbContext.cs")).Count() <= 220);
            Assert.True(configurationFiles.Length >= 18);
            Assert.All(
                configurationFiles,
                file => Assert.Contains(
                    "IEntityTypeConfiguration<",
                    ReadRepositoryFile(file),
                    StringComparison.Ordinal));
            Assert.Equal(3, Regex.Matches(dbContextSource, "modelBuilder\\.Entity").Count);
        }

        [Fact]
        public void CommerceNodeDevelopmentSeeder_IsSplitBySeedStepAfterPhase7B()
        {
            var seederDirectory = RepositoryPath("BlazorShop.Infrastructure/Data/CommerceNode");
            var files = Directory.EnumerateFiles(seederDirectory, "CommerceNodeDevelopmentSeeder*.cs")
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Contains("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.StoreSeed.cs", files);
            Assert.Contains("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.SettingsSeed.cs", files);
            Assert.Contains("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.CatalogSeed.cs", files);
            Assert.Contains("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.MediaSeed.cs", files);
            Assert.Contains("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.ContentNavigationSeed.cs", files);
            Assert.Contains("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.AccountOrderSeed.cs", files);

            Assert.Contains(
                "ICommerceNodeDevelopmentSeedStep",
                ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.cs"),
                StringComparison.Ordinal);
            Assert.All(
                files,
                file => Assert.True(
                    File.ReadLines(RepositoryPath(file)).Count() <= 650,
                    $"{file} should stay below the Phase 7B split threshold."));
        }

        [Fact]
        public void CommerceNodeSwaggerExtensions_IsSplitByFeatureAfterPhase7A()
        {
            var swaggerDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger");
            var files = Directory.EnumerateFiles(swaggerDirectory, "CommerceNodeSwagger*.cs")
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwagger.StoreAdminOperationMetadataFilter.cs",
                files);
            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwagger.StorefrontOperationMetadataFilter.cs",
                files);
            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerResponseHelpers.cs",
                files);

            Assert.True(
                File.ReadLines(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs")).Count() <= 250);
            Assert.All(
                files,
                file => Assert.True(
                    File.ReadLines(RepositoryPath(file)).Count() <= 650,
                    $"{file} should stay below the Phase 7A split threshold."));
        }

        [Fact]
        public void ActiveV2BuildAndTestSurface_IsIsolatedFromLegacyAfterPhase8()
        {
            var v2TestProject = ReadRepositoryFile("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj");
            var solution = ReadRepositoryFile("BlazorShop.sln")
                .Replace(@"\\", "/", StringComparison.Ordinal)
                .Replace('\\', '/');

            Assert.False(File.Exists(RepositoryPath("BlazorShop.Tests/BlazorShop.Tests.csproj")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.V2.slnf")));
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.Presentation")));
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.AppHost")));
            Assert.DoesNotContain("BlazorShop.Presentation\\", v2TestProject, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"..\BlazorShop.Tests\", v2TestProject, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BlazorShop.Presentation/", solution, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BlazorShop.AppHost", solution, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BlazorShop.Tests/BlazorShop.Tests.csproj", solution, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj", solution, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2\\BlazorShop.ControlPlane.API", v2TestProject, StringComparison.Ordinal);
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
