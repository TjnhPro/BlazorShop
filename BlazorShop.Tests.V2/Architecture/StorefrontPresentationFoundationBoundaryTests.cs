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
            var viewSet = CreateValidViewSet();
            var missingProduct = new StorefrontFoundationViewSet
            {
                ApplicationHead = viewSet.ApplicationHead,
                VisualScripts = viewSet.VisualScripts,
                MainLayout = viewSet.MainLayout,
                ConsentBanner = viewSet.ConsentBanner,
                HomePage = viewSet.HomePage,
                CategoryPage = viewSet.CategoryPage,
                ProductPage = null!,
                SearchPage = viewSet.SearchPage,
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
            var missingCheckout = CopyViewSet(viewSet, omitCheckoutPage: true);
            var missingError = CopyViewSet(viewSet, omitErrorState: true);

            var validator = new StorefrontFoundationViewOptionsValidator();
            var productResult = validator.Validate(null, new StorefrontFoundationViewOptions { ViewSet = missingProduct });
            var checkoutResult = validator.Validate(null, new StorefrontFoundationViewOptions { ViewSet = missingCheckout });
            var errorResult = validator.Validate(null, new StorefrontFoundationViewOptions { ViewSet = missingError });

            Assert.True(productResult.Failed);
            Assert.True(checkoutResult.Failed);
            Assert.True(errorResult.Failed);
            Assert.Contains(productResult.Failures, failure => failure.Contains("ProductPage", StringComparison.Ordinal));
            Assert.Contains(checkoutResult.Failures, failure => failure.Contains("CheckoutPage", StringComparison.Ordinal));
            Assert.Contains(errorResult.Failures, failure => failure.Contains("ErrorState", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenRequiredSlotUsesEmptyFallback()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), productPage: typeof(StorefrontFoundationEmptyView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("StorefrontFoundationEmptyView", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenVisualSlotUsesRouteComponent()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), productPage: typeof(RouteFoundationView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("route component", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenHostVisualScriptsTryToReplaceCoreScripts()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), visualScripts: typeof(StorefrontFoundationCoreScripts)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("Presentation-owned core scripts", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenExpectedContextDoesNotMatchSlot()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), productPage: typeof(WrongContextFoundationView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ProductPage", StringComparison.Ordinal));
            Assert.Contains(result.Failures, failure => failure.Contains("StorefrontProductPageContext", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenExpectedContextParameterIsMissing()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), productPage: typeof(MissingContextFoundationView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ProductPage", StringComparison.Ordinal));
            Assert.Contains(result.Failures, failure => failure.Contains("[Parameter]", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenApplicationHeadContextParameterIsMissing()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), applicationHead: typeof(MissingContextFoundationView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ApplicationHead", StringComparison.Ordinal));
            Assert.Contains(result.Failures, failure => failure.Contains("StorefrontShellContext", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenMainLayoutBodyParameterIsMissing()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), mainLayout: typeof(ValidFoundationView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("MainLayout", StringComparison.Ordinal));
            Assert.Contains(result.Failures, failure => failure.Contains("Body", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenConsentBannerContextParameterIsMissing()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), consentBanner: typeof(MissingContextFoundationView)),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ConsentBanner", StringComparison.Ordinal));
            Assert.Contains(result.Failures, failure => failure.Contains("StorefrontConsentContext", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenConsentBannerIsMissing()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), omitConsentBanner: true),
                });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ConsentBanner", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewOptionsValidator_PassesWhenContextTypeIsAssignable()
        {
            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions
                {
                    ViewSet = CopyViewSet(CreateValidViewSet(), productPage: typeof(AssignableContextFoundationView)),
                });

            Assert.False(result.Failed);
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
            var v2Registration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs");
            Assert.DoesNotContain(
                "StorefrontFoundationViewSet.CreateMinimal",
                v2Registration,
                StringComparison.Ordinal);
            Assert.Contains("ErrorState = typeof(ErrorState)", v2Registration, StringComparison.Ordinal);
            Assert.Contains("ConsentBanner = typeof(StorefrontConsentBanner)", v2Registration, StringComparison.Ordinal);
            Assert.Contains(
                "AddStarterFoundationViews",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"),
                StringComparison.Ordinal);
            var starterRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs");
            Assert.DoesNotContain(
                "StorefrontFoundationViewSet.CreateMinimal",
                starterRegistration,
                StringComparison.Ordinal);
            Assert.Contains("VisualScripts = typeof(ApplicationScripts)", starterRegistration, StringComparison.Ordinal);
            Assert.Contains("ErrorState = typeof(ErrorState)", starterRegistration, StringComparison.Ordinal);
            Assert.Contains("ConsentBanner = typeof(StarterConsentBanner)", starterRegistration, StringComparison.Ordinal);
            Assert.Contains(
                "MapStorefrontApplication(",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "MapStorefrontApplication(",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"),
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "AdditionalAssemblies",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "DefaultLayout=\"@typeof(StorefrontFoundationLayout)\"",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewOutlet",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor"),
                StringComparison.Ordinal);
        }

        [Fact]
        public void StarterSource_DoesNotUseThemePagesTerminology()
        {
            var offenders = EnumerateSourceFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter")
                .Select(file => (File: file, Source: ReadRepositoryFile(file)))
                .Where(file => file.Source.Contains(".Theme.Pages", StringComparison.Ordinal)
                    || file.Source.Contains("Theme/Pages", StringComparison.Ordinal)
                    || file.Source.Contains("Theme\\Pages", StringComparison.Ordinal))
                .Select(file => file.File)
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
        }

        [Fact]
        public void FoundationViewSet_DoesNotExposeMinimalProductionEscapeHatch()
        {
            var viewSet = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs");
            var productionCallers = EnumerateSourceFiles("BlazorShop.PresentationV2")
                .Where(file => !file.Contains("/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs", StringComparison.Ordinal))
                .Select(file => (File: file, Source: ReadRepositoryFile(file)))
                .Where(file => file.Source.Contains("CreateMinimal", StringComparison.Ordinal))
                .Select(file => file.File)
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.DoesNotContain("public static StorefrontFoundationViewSet CreateMinimal", viewSet, StringComparison.Ordinal);
            Assert.DoesNotContain("public Type ApplicationScripts", viewSet, StringComparison.Ordinal);
            Assert.Empty(productionCallers);
        }

        [Fact]
        public void FoundationViewSet_DoesNotExposeDeletedCollectionPageSlots()
        {
            var viewSet = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs");
            var validator = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewOptionsValidator.cs");

            Assert.DoesNotContain("DealsPage", viewSet, StringComparison.Ordinal);
            Assert.DoesNotContain("NewReleasesPage", viewSet, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontDealsPageContext", validator, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontNewReleasesPageContext", validator, StringComparison.Ordinal);
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
        public async Task PageStatePolicy_MapsStatusAndPrivateHeaders()
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
            await notFoundContext.Response.StartAsync();
            await serviceUnavailableContext.Response.StartAsync();

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

            Assert.Contains("app.MapStorefrontApplication(", v2Program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontApplication(", starterProgram, StringComparison.Ordinal);
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
            var view = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor");

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
            Assert.Contains("<StorefrontProductGallery", view, StringComparison.Ordinal);
            Assert.Contains("Labels=\"ProductGalleryVisuals.Labels\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes=\"ProductGalleryVisuals.Classes\"", view, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductPricing", view, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductAvailability", view, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductPurchasePanel Model=\"_purchasePanel\"", view, StringComparison.Ordinal);
            Assert.Contains("Labels=\"ProductPurchasePanelVisuals.Labels\"", view, StringComparison.Ordinal);
            Assert.Contains("<StorefrontProductVariantList", view, StringComparison.Ordinal);
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

        private static IEnumerable<string> EnumerateSourceFiles(string relativeFolder)
        {
            var root = FindRepositoryRoot();
            var absoluteFolder = Path.Combine(root, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            return Directory.Exists(absoluteFolder)
                ? Directory.EnumerateFiles(absoluteFolder, "*.*", SearchOption.AllDirectories)
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && Path.GetExtension(path) is ".cs" or ".razor" or ".csproj" or ".props" or ".json" or ".yaml" or ".yml" or ".md")
                    .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                : [];
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

        private static StorefrontFoundationViewSet CreateValidViewSet()
        {
            return new StorefrontFoundationViewSet
            {
                ApplicationHead = typeof(ValidLayoutFoundationView),
                VisualScripts = typeof(ValidLayoutFoundationView),
                MainLayout = typeof(ValidLayoutFoundationView),
                ConsentBanner = typeof(ValidLayoutFoundationView),
                HomePage = typeof(ValidLayoutFoundationView),
                CategoryPage = typeof(ValidLayoutFoundationView),
                ProductPage = typeof(ValidLayoutFoundationView),
                SearchPage = typeof(ValidLayoutFoundationView),
                ContentPage = typeof(ValidLayoutFoundationView),
                CartPage = typeof(ValidLayoutFoundationView),
                CheckoutPage = typeof(ValidLayoutFoundationView),
                PaymentResultPage = typeof(ValidLayoutFoundationView),
                AuthPage = typeof(ValidLayoutFoundationView),
                AccountPage = typeof(ValidLayoutFoundationView),
                MaintenanceState = typeof(ValidLayoutFoundationView),
                NotFoundState = typeof(ValidLayoutFoundationView),
                ServiceUnavailableState = typeof(ValidLayoutFoundationView),
                ErrorState = typeof(ValidLayoutFoundationView),
            };
        }

        private static StorefrontFoundationViewSet CopyViewSet(
            StorefrontFoundationViewSet source,
            Type? applicationHead = null,
            Type? visualScripts = null,
            Type? mainLayout = null,
            Type? consentBanner = null,
            Type? productPage = null,
            Type? checkoutPage = null,
            Type? errorState = null,
            bool omitConsentBanner = false,
            bool omitCheckoutPage = false,
            bool omitErrorState = false)
        {
            return new StorefrontFoundationViewSet
            {
                ApplicationHead = applicationHead ?? source.ApplicationHead,
                VisualScripts = visualScripts ?? source.VisualScripts,
                MainLayout = mainLayout ?? source.MainLayout,
                ConsentBanner = omitConsentBanner ? null! : consentBanner ?? source.ConsentBanner,
                HomePage = source.HomePage,
                CategoryPage = source.CategoryPage,
                ProductPage = productPage ?? source.ProductPage,
                SearchPage = source.SearchPage,
                ContentPage = source.ContentPage,
                CartPage = source.CartPage,
                CheckoutPage = omitCheckoutPage ? null! : checkoutPage ?? source.CheckoutPage,
                PaymentResultPage = source.PaymentResultPage,
                AuthPage = source.AuthPage,
                AccountPage = source.AccountPage,
                MaintenanceState = source.MaintenanceState,
                NotFoundState = source.NotFoundState,
                ServiceUnavailableState = source.ServiceUnavailableState,
                ErrorState = omitErrorState ? null! : errorState ?? source.ErrorState,
            };
        }

        private class ValidFoundationView : IComponent
        {
            [Parameter]
            public object Context { get; set; } = default!;

            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class ValidLayoutFoundationView : ValidFoundationView
        {
            [Parameter]
            public RenderFragment Body { get; set; } = default!;
        }

        [Route("/route-foundation-view")]
        private sealed class RouteFoundationView : ValidFoundationView
        {
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

        private sealed class AssignableContextFoundationView : IComponent
        {
            [Parameter]
            public object Context { get; set; } = default!;

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
