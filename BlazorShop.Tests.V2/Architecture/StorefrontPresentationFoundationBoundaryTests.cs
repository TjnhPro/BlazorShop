namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;

    using BlazorShop.Storefront.Presentation.Views.Foundation;
    using BlazorShop.Storefront.Presentation.PagePatterns;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Http;

    using Xunit;

    public sealed class StorefrontPresentationFoundationBoundaryTests
    {
        [Fact]
        public void PresentationProject_ReferencesOnlyRuntimeAndComponents()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");

            Assert.Equal(
                [
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj",
                ],
                references);
        }

        [Fact]
        public void PresentationProject_DoesNotReferenceHostOrBackendProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");
            var forbidden = references
                .Where(reference =>
                    reference.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Storefront.V2.WASM/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Storefront.Starter/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.ServiceDefaults/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.Empty(forbidden);
        }

        [Fact]
        public void BrowserAndComponentProjects_DoNotDependOnPresentationRuntimeOrClient()
        {
            var wasmReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");
            var componentReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");

            Assert.DoesNotContain(wasmReferences, IsPresentationRuntimeOrClientReference);
            Assert.DoesNotContain(componentReferences, IsPresentationRuntimeOrClientReference);
        }

        [Fact]
        public void ComponentsProject_RemainsLogicOnlyWithoutRazorFiles()
        {
            var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var razorFiles = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(razorFiles);
        }

        [Fact]
        public void PresentationProject_IsInSolutionAndHasFoundationFolders()
        {
            var solution = ReadRepositoryFile("BlazorShop.sln");
            Assert.Contains(
                "BlazorShop.PresentationV2\\BlazorShop.Storefront.Presentation\\BlazorShop.Storefront.Presentation.csproj",
                solution,
                StringComparison.Ordinal);

            var expectedFolders = new[]
            {
                "App",
                "Routing",
                "Pages",
                "PagePatterns",
                "Services",
                "Seo",
                "Endpoints",
                "Security",
                "Hosting",
                "Views/Foundation",
                "DependencyInjection",
            };

            foreach (var expectedFolder in expectedFolders)
            {
                Assert.True(
                    Directory.Exists(RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/{expectedFolder}")),
                    $"{expectedFolder} must exist in the Presentation foundation project.");
            }
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenRequiredSlotIsMissing()
        {
            var viewSet = StorefrontFoundationViewSet.CreateMinimal(typeof(ValidFoundationView));
            viewSet = new StorefrontFoundationViewSet
            {
                ApplicationHead = viewSet.ApplicationHead,
                ApplicationScripts = viewSet.ApplicationScripts,
                MainLayout = viewSet.MainLayout,
                HomePage = viewSet.HomePage,
                CategoryPage = viewSet.CategoryPage,
                ProductPage = null!,
                SearchPage = viewSet.SearchPage,
                DealsPage = viewSet.DealsPage,
                NewReleasesPage = viewSet.NewReleasesPage,
                ContentPage = viewSet.ContentPage,
                CartPage = viewSet.CartPage,
                CheckoutPage = viewSet.CheckoutPage,
                PaymentResultPage = viewSet.PaymentResultPage,
                AuthPage = viewSet.AuthPage,
                AccountPage = viewSet.AccountPage,
                MaintenanceState = viewSet.MaintenanceState,
                NotFoundState = viewSet.NotFoundState,
                ServiceUnavailableState = viewSet.ServiceUnavailableState,
                ErrorState = viewSet.ErrorState,
            };

            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions { ViewSet = viewSet });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ProductPage", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewTypeValidator_RequiresContextParameterWhenContextIsProvided()
        {
            var context = new FoundationContext("demo");

            StorefrontFoundationViewTypeValidator.Validate(typeof(ValidFoundationView), context);

            Assert.Throws<InvalidOperationException>(() =>
                StorefrontFoundationViewTypeValidator.Validate(typeof(MissingContextFoundationView), context));
            Assert.Throws<InvalidOperationException>(() =>
                StorefrontFoundationViewTypeValidator.Validate(typeof(WrongContextFoundationView), context));
        }

        [Fact]
        public void V2AndStarter_RegisterFoundationViewSetsWithoutOwningPresentation()
        {
            var v2References = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");
            var starterReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");

            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                v2References);
            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                starterReferences);

            Assert.Contains(
                "AddV2FoundationViews",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewSet.CreateMinimal",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "AddStarterFoundationViews",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewSet.CreateMinimal",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "MapRazorComponents<StorefrontApp>()",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "MapRazorComponents<StorefrontApp>()",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "AdditionalAssemblies",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "DefaultLayout=\"@ViewSet.MainLayout\"",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewOutlet",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor"),
                StringComparison.Ordinal);
        }

        [Fact]
        public void V2AndStarter_RetireOldAppAndRoutesRootFiles()
        {
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Routes.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/App.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Routes.razor")));
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor")));
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor")));
        }

        [Fact]
        public void PageStateMapper_RequiresSeoDocumentForReadyState()
        {
            Assert.Throws<InvalidOperationException>(() =>
                StorefrontPageResultMapper.Ready(
                    StorefrontPageKind.Home,
                    new FoundationContext("demo"),
                    new StorefrontPageDocument()));
        }

        [Fact]
        public void PageStatePolicy_MapsStatusAndPrivateHeaders()
        {
            var ready = StorefrontPageResultMapper.Ready(
                StorefrontPageKind.Product,
                new FoundationContext("demo"),
                new StorefrontPageDocument("Product", "Description", "/product/demo", RobotsIndex: false, RobotsFollow: false),
                httpStatusCode: 418,
                retryable: true);
            var notFound = StorefrontPageResultMapper.NotFound(StorefrontPageKind.Product);
            var serviceUnavailable = StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.Product);

            var readyContext = new DefaultHttpContext();
            var notFoundContext = new DefaultHttpContext();
            var serviceUnavailableContext = new DefaultHttpContext();

            StorefrontResponseHeaders.ApplyStatus(readyContext, ready);
            StorefrontResponseHeaders.ApplyStatus(notFoundContext, notFound);
            StorefrontResponseHeaders.ApplyStatus(serviceUnavailableContext, serviceUnavailable);

            Assert.Equal(418, readyContext.Response.StatusCode);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundContext.Response.StatusCode);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, serviceUnavailableContext.Response.StatusCode);
            Assert.Equal(StorefrontHttpStatusPolicy.NoIndexNoFollow, readyContext.Response.Headers["X-Robots-Tag"].ToString());
            Assert.Equal(StorefrontHttpStatusPolicy.PrivateCacheControl, readyContext.Response.Headers["Cache-Control"].ToString());
        }

        [Fact]
        public void PresentationPageShell_IsWiredToHeadAndStatusPolicy()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/PagePatterns/StorefrontPage.razor");

            Assert.Contains("@typeparam TContext", source, StringComparison.Ordinal);
            Assert.Contains("<StorefrontSeoHead", source, StringComparison.Ordinal);
            Assert.Contains("StorefrontResponseHeaders.ApplyStatus", source, StringComparison.Ordinal);
            Assert.Contains("CurrentState is StorefrontPageState.Ready<TContext>", source, StringComparison.Ordinal);
            Assert.Contains("LoadingContent", source, StringComparison.Ordinal);
            Assert.Contains("EmptyContent", source, StringComparison.Ordinal);
            Assert.Contains("NotFoundContent", source, StringComparison.Ordinal);
            Assert.Contains("ServiceUnavailableContent", source, StringComparison.Ordinal);
            Assert.Contains("UnauthorizedContent", source, StringComparison.Ordinal);
            Assert.Contains("MaintenanceContent", source, StringComparison.Ordinal);
            Assert.Contains("ErrorContent", source, StringComparison.Ordinal);
        }

        [Fact]
        public void SeoAndDiscoveryServices_AreOwnedByPresentation()
        {
            var expectedPresentationFiles = new[]
            {
                "Models/StorefrontCatalogContentModels.cs",
                "Options/StorefrontPublicUrlOptions.cs",
                "Configuration/StorefrontPublicUrlOptionsValidator.cs",
                "Services/StorefrontApiResult.cs",
                "Services/StorefrontIndexingPolicy.cs",
                "Services/StorefrontRoutes.cs",
                "Services/Contracts/IStorefrontPublicUrlResolver.cs",
                "Services/Contracts/IStorefrontRobotsService.cs",
                "Services/Contracts/IStorefrontSeoComposer.cs",
                "Services/Contracts/IStorefrontSeoDiscoveryReaders.cs",
                "Services/Contracts/IStorefrontSeoSettingsProvider.cs",
                "Services/Contracts/IStorefrontSitemapService.cs",
                "Services/Contracts/IStorefrontStructuredDataComposer.cs",
                "Seo/SeoRuntimeLogger.cs",
                "Seo/StorefrontPublicUrlResolver.cs",
                "Seo/StorefrontRobotsService.cs",
                "Seo/StorefrontRuntimeSeoDiscoveryReaders.cs",
                "Seo/StorefrontSeoComposer.cs",
                "Seo/StorefrontSeoSettingsProvider.cs",
                "Seo/StorefrontSitemapService.cs",
                "Seo/StorefrontStructuredDataComposer.cs",
                "Seo/StorefrontStructuredDataDocument.cs",
            };

            foreach (var relativeFile in expectedPresentationFiles)
            {
                Assert.True(
                    File.Exists(RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/{relativeFile}")),
                    $"{relativeFile} must be owned by BlazorShop.Storefront.Presentation.");
            }

            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontSeoEndpoints.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Endpoints/StarterSeoEndpoints.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSeoComposer.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSitemapService.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontRobotsService.cs")));
        }

        [Fact]
        public void V2AndStarter_MapSharedPresentationSeoEndpoints()
        {
            var v2Program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var starterProgram = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs");
            var presentationEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationSeoEndpoints.cs");

            Assert.Contains("app.MapStorefrontPresentation();", v2Program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentation();", starterProgram, StringComparison.Ordinal);
            Assert.DoesNotContain("MapStorefrontSeoEndpoints", v2Program, StringComparison.Ordinal);
            Assert.DoesNotContain("MapStarterSeoEndpoints", starterProgram, StringComparison.Ordinal);
            Assert.Contains("MapStorefrontPresentationSeoEndpoints", presentationEndpoints, StringComparison.Ordinal);
            Assert.Contains("StorefrontRoutes.Robots", presentationEndpoints, StringComparison.Ordinal);
            Assert.Contains("StorefrontRoutes.Sitemap", presentationEndpoints, StringComparison.Ordinal);
            Assert.Contains("StorefrontResponseHeaders.ApplyRobotsDocument", presentationEndpoints, StringComparison.Ordinal);
            Assert.Contains("StorefrontResponseHeaders.ApplySitemapDocument", presentationEndpoints, StringComparison.Ordinal);
        }

        [Fact]
        public void SeoHeadComponents_ArePresentationOwnedWhileV2KeepsOnlyBrandHead()
        {
            var v2SeoFiles = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo"), "*.razor", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileName(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var v2Pages = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages"), "*.razor", SearchOption.AllDirectories)
                .Select(File.ReadAllText)
                .ToArray();
            var presentationPages = Directory
                .EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages"), "*.razor", SearchOption.AllDirectories)
                .Select(File.ReadAllText)
                .ToArray();

            Assert.Equal(["StorefrontBrandHead.razor"], v2SeoFiles);
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Seo/StorefrontSeoHead.razor")));
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Seo/StorefrontJsonLdScript.razor")));
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Seo/StaticPageSeo.razor")));
            Assert.All(v2Pages, markup => Assert.DoesNotContain("<SeoHead", markup, StringComparison.Ordinal));
            var pageShell = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/PagePatterns/StorefrontPage.razor");
            Assert.Contains("<StorefrontSeoHead", pageShell, StringComparison.Ordinal);
            Assert.DoesNotContain(presentationPages, markup => markup.Contains("<StorefrontSeoHead", StringComparison.Ordinal));
        }

        [Fact]
        public void ProductPageVerticalSlice_IsPresentationRouteWithV2ViewOnly()
        {
            var route = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Hybrid/Catalog/ProductRoutePage.razor");
            var service = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageService.cs");
            var mapper = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageMapper.cs");
            var view = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor");

            Assert.Contains("@page \"/product/{Slug}\"", route, StringComparison.Ordinal);
            Assert.Contains("StorefrontProductPageService", route, StringComparison.Ordinal);
            Assert.Contains("<StorefrontPage TContext=\"StorefrontProductPageContext\"", route, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontSeoHead", route, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontResponseHeaders.ApplyStatus", route, StringComparison.Ordinal);
            Assert.Contains("IStorefrontCatalogClient", service, StringComparison.Ordinal);
            Assert.Contains("IStorefrontSeoComposer", service, StringComparison.Ordinal);
            Assert.Contains("IStorefrontStructuredDataComposer", service, StringComparison.Ordinal);
            Assert.Contains("StorefrontProductPageMapper.Map", service, StringComparison.Ordinal);
            Assert.Contains("BuildPurchasePanel", mapper, StringComparison.Ordinal);
            Assert.Contains("BuildGalleryItems", mapper, StringComparison.Ordinal);
            Assert.DoesNotContain("@page", view, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontCatalogClient", view, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontSeoComposer", view, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontStructuredDataComposer", view, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontResponseHeaders", view, StringComparison.Ordinal);
            Assert.Contains("StorefrontProductPageContext", view, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductGallery Items=\"_galleryItems\" ProductName=\"@_product.Name\" />", view, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductPurchasePanel Model=\"_purchasePanel\" />", view, StringComparison.Ordinal);
        }

        private static bool IsPresentationRuntimeOrClientReference(string reference)
        {
            return reference.Contains("/BlazorShop.Storefront.Presentation/", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("/BlazorShop.Storefront.Runtime/", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("/BlazorShop.Storefront.Client/", StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
        {
            var projectPath = RepositoryPath(relativeProjectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new DirectoryNotFoundException($"Could not resolve project directory for {relativeProjectPath}.");
            var document = XDocument.Load(projectPath);

            return document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
                .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToRepositoryRelativePath(string path)
        {
            return Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/');
        }

        private static string FindRepositoryRoot()
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

        private sealed record FoundationContext(string Name);

        private sealed class ValidFoundationView : IComponent
        {
            [Parameter]
            public FoundationContext Context { get; set; } = default!;

            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class MissingContextFoundationView : IComponent
        {
            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class WrongContextFoundationView : IComponent
        {
            [Parameter]
            public string Context { get; set; } = string.Empty;

            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }
    }
}
