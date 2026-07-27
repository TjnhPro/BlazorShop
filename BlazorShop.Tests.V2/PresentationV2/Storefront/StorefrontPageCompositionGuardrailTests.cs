extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontPageCompositionGuardrailTests
    {
        [Theory]
        [InlineData("About.razor")]
        [InlineData("Faq.razor")]
        [InlineData("FAQ.razor")]
        [InlineData("Privacy.razor")]
        [InlineData("Terms.razor")]
        [InlineData("CustomerService.razor")]
        public void ContentPages_DoNotReappearAsDedicatedRouteFiles(string fileName)
        {
            var pageFiles = EnumerateStorefrontPageFiles()
                .Select(Path.GetFileName)
                .ToList();

            Assert.DoesNotContain(pageFiles, name => string.Equals(name, fileName, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("AccountHostPage.razor", "@page \"/account\"")]
        [InlineData("AccountHostPage.razor", "@page \"/account/{*Path}\"")]
        public void RoutePages_KeepExpectedRouteDeclarations(string fileName, string routeDeclaration)
        {
            var pagePath = FindStorefrontPageFile(fileName);

            Assert.NotNull(pagePath);
            Assert.Contains(routeDeclaration, File.ReadAllText(pagePath!), StringComparison.Ordinal);
        }

        [Fact]
        public void PageInventory_RecordsCurrentRenderingOwnershipBaseline()
        {
            var expected = new[]
            {
                new PageInventoryItem("Pages/WasmHost/Account/AccountHostPage.razor", "/account", RenderOwnership.WasmHost),
                new PageInventoryItem("Pages/WasmHost/Account/AccountHostPage.razor", "/account/{*Path}", RenderOwnership.WasmHost),
            };

            var pageRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2");

            foreach (var item in expected)
            {
                var pagePath = Path.Combine(pageRoot, item.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(pagePath), $"{item.RelativePath} must remain in the baseline inventory.");

                var markup = File.ReadAllText(pagePath);
                Assert.Contains($"@page \"{item.Route}\"", markup, StringComparison.Ordinal);
            }

            Assert.Equal(
                [RenderOwnership.WasmHost],
                expected.Select(item => item.Ownership).Distinct().OrderBy(item => item.ToString()).ToArray());
        }

        [Fact]
        public void ContentSystemAuthAndCartRoutes_ArePresentationOwnedAndV2ProvidesViews()
        {
            var routes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Pages/Ssr/Cart/CartRoutePage.razor"] = "@page \"/my-cart\"",
                ["Pages/Hybrid/Commerce/CheckoutRoutePage.razor"] = "@page \"/checkout\"",
                ["Pages/Hybrid/Commerce/PaymentResultRoutePage.razor"] = "@page \"/payment-success\"",
                ["Pages/Hybrid/Commerce/PaymentResultRoutePage.razor|cancel"] = "@page \"/payment-cancel\"",
                ["Pages/Hybrid/Commerce/PaymentResultRoutePage.razor|result"] = "@page \"/payment/result\"",
                ["Pages/Ssr/Content/ContentRoutePage.razor"] = "@page \"/pages/{Slug}\"",
                ["Pages/Ssr/Auth/SignInRoutePage.razor"] = "@page \"/signin\"",
                ["Pages/Ssr/Auth/RegisterRoutePage.razor"] = "@page \"/register\"",
                ["Pages/Ssr/Auth/ForgotPasswordRoutePage.razor"] = "@page \"/forgot-password\"",
                ["Pages/Ssr/Auth/ResetPasswordRoutePage.razor"] = "@page \"/reset-password\"",
                ["Pages/Ssr/System/MaintenanceRoutePage.razor"] = "@page \"/maintenance\"",
                ["Pages/Ssr/System/NotFoundRoutePage.razor"] = "@page \"/{*Path:nonfile}\"",
            };
            var presentationRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation");
            var registration = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs"));
            var program = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"));
            var v2AuthFormEndpoints = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAuthFormEndpoints.cs"));

            foreach (var route in routes)
            {
                var routePath = route.Key.Split('|')[0];
                var routeMarkup = File.ReadAllText(Path.Combine(presentationRoot, routePath.Replace('/', Path.DirectorySeparatorChar)));
                Assert.Contains(route.Value, routeMarkup, StringComparison.Ordinal);
            }

            var viewPaths = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/PaymentResultPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Auth/V2AuthPageView.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/System/MaintenancePage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/System/NotFoundPage.razor",
            };

            foreach (var viewPath in viewPaths)
            {
                var viewMarkup = File.ReadAllText(RepositoryPath(viewPath));
                Assert.DoesNotContain("@page \"", viewMarkup, StringComparison.Ordinal);
                Assert.Contains(" Context", viewMarkup, StringComparison.Ordinal);
            }

            Assert.Contains("@page \"/cart\"", File.ReadAllText(Path.Combine(presentationRoot, "Pages/Ssr/Cart/CartRoutePage.razor".Replace('/', Path.DirectorySeparatorChar))), StringComparison.Ordinal);
            Assert.Contains("CartPage = typeof(CartPage)", registration, StringComparison.Ordinal);
            Assert.Contains("CheckoutPage = typeof(CheckoutPage)", registration, StringComparison.Ordinal);
            Assert.Contains("PaymentResultPage = typeof(PaymentResultPage)", registration, StringComparison.Ordinal);
            Assert.Contains("ContentPage = typeof(StorefrontPage)", registration, StringComparison.Ordinal);
            Assert.Contains("AuthPage = typeof(V2AuthPageView)", registration, StringComparison.Ordinal);
            Assert.Contains("MaintenanceState = typeof(MaintenancePage)", registration, StringComparison.Ordinal);
            Assert.Contains("NotFoundState = typeof(NotFoundPage)", registration, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentationAuthEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentationCartEndpoints();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentationCheckoutEndpoints();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(StorefrontRoutes.SignIn", v2AuthFormEndpoints, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(StorefrontRoutes.Register", v2AuthFormEndpoints, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(StorefrontRoutes.ForgotPassword", v2AuthFormEndpoints, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(StorefrontRoutes.ResetPassword", v2AuthFormEndpoints, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(StorefrontRoutes.Logout", v2AuthFormEndpoints, StringComparison.Ordinal);
        }

        [Fact]
        public void CatalogRoutes_ArePresentationOwnedAndV2ProvidesViews()
        {
            var routes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["HomeRoutePage.razor"] = "@page \"/\"",
                ["CategoryRoutePage.razor"] = "@page \"/category/{Slug}\"",
                ["SearchRoutePage.razor"] = "@page \"/search\"",
                ["TodaysDealsRoutePage.razor"] = "@page \"/todays-deals\"",
                ["NewReleasesRoutePage.razor"] = "@page \"/new-releases\"",
            };
            var presentationCatalogRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Hybrid/Catalog");
            var registration = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs"));

            foreach (var route in routes)
            {
                var routeMarkup = File.ReadAllText(Path.Combine(presentationCatalogRoot, route.Key));
                Assert.Contains(route.Value, routeMarkup, StringComparison.Ordinal);
                Assert.Contains("StorefrontSeoHead", routeMarkup, StringComparison.Ordinal);
                Assert.Contains("StorefrontResponseHeaders.ApplyStatus", routeMarkup, StringComparison.Ordinal);
            }

            var viewPaths = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/TodaysDeals.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/NewReleases.razor",
            };

            foreach (var viewPath in viewPaths)
            {
                var viewMarkup = File.ReadAllText(RepositoryPath(viewPath));
                Assert.DoesNotContain("@page \"", viewMarkup, StringComparison.Ordinal);
                Assert.DoesNotContain("IStorefrontCatalogClient", viewMarkup, StringComparison.Ordinal);
                Assert.DoesNotContain("IStorefrontSeoComposer", viewMarkup, StringComparison.Ordinal);
                Assert.DoesNotContain("StorefrontResponseHeaders", viewMarkup, StringComparison.Ordinal);
            }

            Assert.Contains("HomePage = typeof(Home)", registration, StringComparison.Ordinal);
            Assert.Contains("CategoryPage = typeof(CategoryPage)", registration, StringComparison.Ordinal);
            Assert.Contains("SearchPage = typeof(SearchPage)", registration, StringComparison.Ordinal);
            Assert.Contains("DealsPage = typeof(TodaysDeals)", registration, StringComparison.Ordinal);
            Assert.Contains("NewReleasesPage = typeof(NewReleases)", registration, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductRoute_IsPresentationOwnedAndV2ProvidesView()
        {
            var route = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Hybrid/Catalog/ProductRoutePage.razor"));
            var view = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor"));
            var registration = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs"));

            Assert.Contains("@page \"/product/{Slug}\"", route, StringComparison.Ordinal);
            Assert.Contains("StorefrontProductPageService", route, StringComparison.Ordinal);
            Assert.Contains("ComponentType=\"@ViewSet.ProductPage\"", route, StringComparison.Ordinal);
            Assert.Contains("ProductPage = typeof(V2ProductPageView)", registration, StringComparison.Ordinal);
            Assert.DoesNotContain("@page", view, StringComparison.Ordinal);
            Assert.Contains("public StorefrontProductPageContext Context", view, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterPageInventory_RecordsCurrentSecondConsumerBaseline()
        {
            var expected = new[]
            {
                new PageInventoryItem("Pages/Hybrid/Catalog/ProductPage.razor", "/product/{Slug}", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/WasmHost/Account/AccountHostPage.razor", "/account", RenderOwnership.WasmHost),
                new PageInventoryItem("Pages/WasmHost/Account/AccountHostPage.razor", "/account/{*Path}", RenderOwnership.WasmHost),
            };

            var pageRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter");

            foreach (var item in expected)
            {
                var pagePath = Path.Combine(pageRoot, item.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(pagePath), $"{item.RelativePath} must remain in the Starter baseline inventory.");

                var markup = File.ReadAllText(pagePath);
                Assert.Contains($"@page \"{item.Route}\"", markup, StringComparison.Ordinal);
            }

            var starterCartView = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CartPage.razor"));
            var starterCheckoutView = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CheckoutPage.razor"));
            var starterPaymentResultView = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/PaymentResultPage.razor"));
            Assert.DoesNotContain("@page \"", starterCartView, StringComparison.Ordinal);
            Assert.Contains("StorefrontCartPageContext", starterCartView, StringComparison.Ordinal);
            Assert.DoesNotContain("@page \"", starterCheckoutView, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckoutPageContext", starterCheckoutView, StringComparison.Ordinal);
            Assert.DoesNotContain("@page \"", starterPaymentResultView, StringComparison.Ordinal);
            Assert.Contains("StorefrontPaymentResultPageContext", starterPaymentResultView, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontBrowserProjects_KeepPortableDependencyBoundary()
        {
            var componentReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");
            var wasmReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");

            Assert.DoesNotContain(componentReferences, IsForbiddenStorefrontBrowserReference);
            Assert.DoesNotContain(wasmReferences, IsForbiddenStorefrontBrowserReference);

            Assert.Contains(
                wasmReferences,
                reference => reference.EndsWith(
                    "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void StorefrontComponents_OnlyExposeContractsHeadlessAndBrowserFolders()
        {
            var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var allowedRootDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Browser",
                "Contracts",
                "Headless",
                "bin",
                "obj",
            };

            var unexpectedDirectories = Directory.GetDirectories(componentRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => !allowedRootDirectories.Contains(name!))
                .Where(name => Directory.EnumerateFileSystemEntries(Path.Combine(componentRoot, name!)).Any())
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Empty(unexpectedDirectories);
        }

        [Fact]
        public void StorefrontComponentFeatures_DoNotDependOnBackendOrRouteContracts()
        {
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features")));
        }

        [Fact]
        public void StorefrontComponentFeatureModels_DoNotExposeAdminOwnedFields()
        {
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features")));
        }

        [Theory]
        [InlineData(nameof(StorefrontRoutes.About), "/pages/about-us")]
        [InlineData(nameof(StorefrontRoutes.Faq), "/pages/faq")]
        [InlineData(nameof(StorefrontRoutes.Privacy), "/pages/privacy")]
        [InlineData(nameof(StorefrontRoutes.Terms), "/pages/terms")]
        [InlineData(nameof(StorefrontRoutes.CustomerService), "/pages/customer-service")]
        public void ContentRouteConstants_PointToDynamicPagesRenderer(string routeName, string expected)
        {
            var actual = routeName switch
            {
                nameof(StorefrontRoutes.About) => StorefrontRoutes.About,
                nameof(StorefrontRoutes.Faq) => StorefrontRoutes.Faq,
                nameof(StorefrontRoutes.Privacy) => StorefrontRoutes.Privacy,
                nameof(StorefrontRoutes.Terms) => StorefrontRoutes.Terms,
                nameof(StorefrontRoutes.CustomerService) => StorefrontRoutes.CustomerService,
                _ => throw new ArgumentOutOfRangeException(nameof(routeName), routeName, null),
            };

            Assert.Equal(expected, actual);
            Assert.StartsWith(StorefrontRoutes.PagesBase + "/", actual, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(StorefrontRoutes.Cart)]
        [InlineData(StorefrontRoutes.Checkout)]
        [InlineData(StorefrontRoutes.SignIn)]
        [InlineData(StorefrontRoutes.Register)]
        [InlineData(StorefrontRoutes.ForgotPassword)]
        [InlineData(StorefrontRoutes.ResetPassword)]
        [InlineData(StorefrontRoutes.Logout)]
        [InlineData(StorefrontRoutes.AccountProfile)]
        [InlineData(StorefrontRoutes.AccountChangePassword)]
        [InlineData(StorefrontRoutes.AccountAddresses)]
        [InlineData(StorefrontRoutes.AccountOrders)]
        [InlineData(StorefrontRoutes.PaymentSuccess)]
        [InlineData(StorefrontRoutes.PaymentCancel)]
        [InlineData(StorefrontRoutes.Maintenance)]
        public void PrivateAndApplicationRoutes_AreNotSitemapStaticRoutes(string route)
        {
            Assert.True(
                StorefrontIndexingPolicy.IsPrivateNoIndexPath(route) || route.StartsWith(StorefrontRoutes.Account, StringComparison.Ordinal),
                $"{route} must be noindex or account-scoped.");

            Assert.DoesNotContain(StorefrontRoutes.SitemapStaticPages, item => string.Equals(item.Path, route, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void SearchRoute_IsNoIndexAndNotASitemapStaticRoute()
        {
            Assert.True(StorefrontIndexingPolicy.IsSearchNoIndexPath(StorefrontRoutes.Search));
            Assert.DoesNotContain(StorefrontRoutes.SitemapStaticPages, item => string.Equals(item.Path, StorefrontRoutes.Search, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DynamicContentRenderer_WiresTemplatePresentationAndStructuredData()
        {
            var routeMarkup = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Ssr/Content/ContentRoutePage.razor"));
            var service = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Content/StorefrontContentPageService.cs"));
            var viewMarkup = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor"));

            Assert.Contains("StorefrontContentPageService", routeMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontSeoHead Metadata=\"_result.Metadata\" StructuredData=\"_result.StructuredData\" />", routeMarkup, StringComparison.Ordinal);
            Assert.Contains("IStorefrontPagePresentationResolver", service, StringComparison.Ordinal);
            Assert.Contains("IStorefrontStructuredDataComposer", service, StringComparison.Ordinal);
            Assert.Contains("presentationResolver.Resolve(page)", service, StringComparison.Ordinal);
            Assert.Contains("ComposeStructuredDataAsync(routePath, page, presentation, metadata", service, StringComparison.Ordinal);
            Assert.Contains("data-storefront-page-template", viewMarkup, StringComparison.Ordinal);
        }

        private static string? FindStorefrontPageFile(string fileName)
        {
            return EnumerateStorefrontPageFiles()
                .SingleOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.Ordinal));
        }

        private static IReadOnlyList<string> EnumerateStorefrontPageFiles()
        {
            return Directory
                .GetFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages"), "*.razor", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
        {
            var projectPath = RepositoryPath(relativeProjectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new DirectoryNotFoundException($"Could not resolve project directory for {relativeProjectPath}.");
            var document = System.Xml.Linq.XDocument.Load(projectPath);

            return document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
                .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsForbiddenStorefrontBrowserReference(string reference)
        {
            var normalized = reference.Replace('\\', '/');

            return normalized.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase);
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        private enum RenderOwnership
        {
            Hybrid,
            Ssr,
            WasmHost
        }

        private sealed record PageInventoryItem(string RelativePath, string Route, RenderOwnership Ownership);
    }
}
