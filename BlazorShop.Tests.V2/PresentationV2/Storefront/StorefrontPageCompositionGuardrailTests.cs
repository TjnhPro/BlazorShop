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
        [InlineData("Home.razor", "@page \"/\"")]
        [InlineData("ProductPage.razor", "@page \"/product/{Slug}\"")]
        [InlineData("CategoryPage.razor", "@page \"/category/{Slug}\"")]
        [InlineData("SearchPage.razor", "@page \"/search\"")]
        [InlineData("NewReleases.razor", "@page \"/new-releases\"")]
        [InlineData("TodaysDeals.razor", "@page \"/todays-deals\"")]
        [InlineData("StorefrontPage.razor", "@page \"/pages/{Slug}\"")]
        [InlineData("CartPage.razor", "@page \"/my-cart\"")]
        [InlineData("CheckoutPage.razor", "@page \"/checkout\"")]
        [InlineData("SignInPage.razor", "@page \"/signin\"")]
        [InlineData("RegisterPage.razor", "@page \"/register\"")]
        [InlineData("ForgotPasswordPage.razor", "@page \"/forgot-password\"")]
        [InlineData("ResetPasswordPage.razor", "@page \"/reset-password\"")]
        [InlineData("PaymentSuccessPage.razor", "@page \"/payment-success\"")]
        [InlineData("PaymentCancelPage.razor", "@page \"/payment-cancel\"")]
        [InlineData("AccountHostPage.razor", "@page \"/account\"")]
        [InlineData("AccountHostPage.razor", "@page \"/account/{*Path}\"")]
        [InlineData("MaintenancePage.razor", "@page \"/maintenance\"")]
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
                new PageInventoryItem("Pages/Hybrid/Catalog/Home.razor", "/", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Catalog/CategoryPage.razor", "/category/{Slug}", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Catalog/ProductPage.razor", "/product/{Slug}", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Catalog/SearchPage.razor", "/search", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Catalog/NewReleases.razor", "/new-releases", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Catalog/TodaysDeals.razor", "/todays-deals", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Ssr/Content/StorefrontPage.razor", "/pages/{Slug}", RenderOwnership.Ssr),
                new PageInventoryItem("Pages/Hybrid/Commerce/CartPage.razor", "/my-cart", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Commerce/CheckoutPage.razor", "/checkout", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Commerce/PaymentSuccessPage.razor", "/payment-success", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Hybrid/Commerce/PaymentCancelPage.razor", "/payment-cancel", RenderOwnership.Hybrid),
                new PageInventoryItem("Pages/Ssr/Auth/SignInPage.razor", "/signin", RenderOwnership.Ssr),
                new PageInventoryItem("Pages/Ssr/Auth/RegisterPage.razor", "/register", RenderOwnership.Ssr),
                new PageInventoryItem("Pages/Ssr/Auth/ForgotPasswordPage.razor", "/forgot-password", RenderOwnership.Ssr),
                new PageInventoryItem("Pages/Ssr/Auth/ResetPasswordPage.razor", "/reset-password", RenderOwnership.Ssr),
                new PageInventoryItem("Pages/WasmHost/Account/AccountHostPage.razor", "/account", RenderOwnership.WasmHost),
                new PageInventoryItem("Pages/WasmHost/Account/AccountHostPage.razor", "/account/{*Path}", RenderOwnership.WasmHost),
                new PageInventoryItem("Pages/Ssr/System/MaintenancePage.razor", "/maintenance", RenderOwnership.Ssr),
                new PageInventoryItem("Pages/Ssr/System/NotFoundPage.razor", "/{*Path:nonfile}", RenderOwnership.Ssr),
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
                [RenderOwnership.Hybrid, RenderOwnership.Ssr, RenderOwnership.WasmHost],
                expected.Select(item => item.Ownership).Distinct().OrderBy(item => item.ToString()).ToArray());
        }

        [Fact]
        public void StorefrontBrowserProjects_KeepPortableDependencyBoundary()
        {
            var componentReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");
            var wasmReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj");

            Assert.DoesNotContain(componentReferences, IsForbiddenStorefrontBrowserReference);
            Assert.DoesNotContain(wasmReferences, IsForbiddenStorefrontBrowserReference);

            Assert.Contains(
                wasmReferences,
                reference => reference.EndsWith(
                    "BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void StorefrontComponents_BusinessFeatureFoldersStayUnderFeatures()
        {
            var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var allowedRootDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Browser",
                "Features",
                "wwwroot",
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
            var markup = File.ReadAllText(FindStorefrontPageFile("StorefrontPage.razor")!);

            Assert.Contains("@inject IStorefrontPagePresentationResolver PresentationResolver", markup, StringComparison.Ordinal);
            Assert.Contains("@inject IStorefrontStructuredDataComposer StructuredDataComposer", markup, StringComparison.Ordinal);
            Assert.Contains("<SeoHead Metadata=\"_metadata\" StructuredData=\"_structuredData\" />", markup, StringComparison.Ordinal);
            Assert.Contains("PresentationResolver.Resolve(_page)", markup, StringComparison.Ordinal);
            Assert.Contains("ComposeStructuredDataAsync(routePath, _page, _presentation)", markup, StringComparison.Ordinal);
            Assert.Contains("data-storefront-page-template", markup, StringComparison.Ordinal);
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
