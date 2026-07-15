namespace BlazorShop.Tests.PresentationV2
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed partial class LayoutAssetFoundationTests
    {
        private static readonly string[] StorefrontRootStylesheetAllowlist = ["css/site.css", "css/storefront.css"];
        private static readonly string[] StorefrontRootScriptAllowlist = ["_framework/blazor.web.js", "js/storefrontCommerce.js"];
        private static readonly string[] ControlPlaneRootStylesheetAllowlist = ["vendor/fontawesome/css/all.min.css", "css/site.css", "css/app.css"];
        private static readonly string[] ControlPlaneRootScriptAllowlist = ["_framework/blazor.webassembly.js", "js/downloads.js"];

        [Fact]
        public void StorefrontRoot_DefinesExpectedAssetsWithoutDuplicates()
        {
            var appMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor");

            Assert.Equal(StorefrontRootStylesheetAllowlist, ExtractStylesheetHrefs(appMarkup));
            Assert.Equal(StorefrontRootScriptAllowlist, ExtractScriptSources(appMarkup));
            Assert.Contains("<link rel=\"icon\" type=\"image/png\" href=\"icon-192.png\" />", appMarkup);
            Assert.True(
                appMarkup.IndexOf("<StorefrontBrandHead />", StringComparison.Ordinal) <
                appMarkup.IndexOf("<HeadOutlet />", StringComparison.Ordinal));
            Assert.True(
                appMarkup.IndexOf("_framework/blazor.web.js", StringComparison.Ordinal) <
                appMarkup.IndexOf("js/storefrontCommerce.js", StringComparison.Ordinal));
            AssertRootDoesNotReferenceLegacyPresentationAssets(appMarkup);
        }

        [Fact]
        public void StorefrontLayout_KeepsSingleToastRegionAndGlobalShell()
        {
            var layoutMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");
            var brandHeadMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");
            var pageShellMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontPageShell.razor");

            Assert.Contains("<StorefrontHeader />", layoutMarkup);
            Assert.Contains("<main class=\"bs-storefront-main flex-1\">", layoutMarkup);
            Assert.Contains("<StorefrontFooter />", layoutMarkup);
            Assert.Equal(1, CountOccurrences(layoutMarkup, "data-storefront-toast-region"));
            Assert.DoesNotContain("<HeadContent>", layoutMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<HeadContent>", brandHeadMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<main", pageShellMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<HeadContent>", pageShellMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontPageShell_DefinesOptionalRegionsWithoutOwningSeoOrMain()
        {
            var pageShellMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontPageShell.razor");

            Assert.Contains("public RenderFragment? Breadcrumb", pageShellMarkup);
            Assert.Contains("public RenderFragment? Header", pageShellMarkup);
            Assert.Contains("public RenderFragment? Actions", pageShellMarkup);
            Assert.Contains("public RenderFragment? Sidebar", pageShellMarkup);
            Assert.Contains("public RenderFragment ChildContent", pageShellMarkup);
            Assert.Contains("[EditorRequired]", pageShellMarkup);
            Assert.DoesNotContain("<main", pageShellMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<SeoHead", pageShellMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<HeadContent>", pageShellMarkup, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CategoryPage.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/StorefrontPage.razor")]
        public void StorefrontRoutedPages_UsePageShellWhileKeepingSeoBreadcrumbAndHeading(string relativePath)
        {
            var pageMarkup = ReadRepositoryFile(relativePath);

            Assert.Contains("<SeoHead", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontPageShell", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<Breadcrumb>", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<BreadcrumbNav", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<ChildContent>", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<h1", pageMarkup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StorefrontCatalogFilterPanel_PreservesQueryStringContract()
        {
            var filterMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor");

            Assert.Contains("method=\"get\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"category\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"q\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"minPrice\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"maxPrice\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"sortBy\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"inStock\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("ProductCatalogSortBy.DisplayOrder.ToApiValue()", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("ProductCatalogSortBy.PriceLowToHigh.ToApiValue()", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("ProductCatalogSortBy.PriceHighToLow.ToApiValue()", filterMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("onclick", filterMarkup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StorefrontCategoryAndSearchPages_UseCatalogFilterPanelWithoutRouteChanges()
        {
            var categoryMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CategoryPage.razor");
            var searchMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/SearchPage.razor");

            Assert.Contains("<CatalogFilterPanel", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowPriceRange=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowSort=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowStock=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("MinPrice=\"MinPrice\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("MaxPrice=\"MaxPrice\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("InStock=\"InStock\"", categoryMarkup, StringComparison.Ordinal);

            Assert.Contains("<CatalogFilterPanel", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("Action=\"@StorefrontRoutes.Search\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowCategory=\"true\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowSearch=\"true\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("SearchTerm=\"Q\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("StorefrontRoutes.SearchUrl(Q, Category, pageNumber)", searchMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontProgram_KeepsStaticAssetMiddleware()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            Assert.Contains("app.UseStaticFiles();", program);
            Assert.Contains("app.MapStaticAssets();", program);
        }

        [Fact]
        public void StorefrontRuntime_DoesNotApplyImmutableCachePolicyToDynamicPipeline()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var responseHeaders = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontResponseHeaders.cs");

            Assert.DoesNotContain("OnPrepareResponse", program, StringComparison.Ordinal);
            Assert.DoesNotContain("max-age=31536000, immutable", program, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("public const string ErrorCacheControl = \"no-store, no-cache, max-age=0\"", responseHeaders);
            Assert.Contains("public const string RobotsCacheControl = \"public, max-age=3600, must-revalidate\"", responseHeaders);
            Assert.Contains("public const string SitemapCacheControl = \"public, max-age=900, must-revalidate\"", responseHeaders);
            Assert.DoesNotContain("immutable", responseHeaders, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ControlPlaneRoot_DefinesExpectedAssetsWithoutDuplicates()
        {
            var indexMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/wwwroot/index.html");

            Assert.Equal(ControlPlaneRootStylesheetAllowlist, ExtractStylesheetHrefs(indexMarkup));
            Assert.Equal(ControlPlaneRootScriptAllowlist, ExtractScriptSources(indexMarkup));
            Assert.Contains("<script type=\"importmap\"></script>", indexMarkup);
            AssertRootDoesNotReferenceLegacyPresentationAssets(indexMarkup);
        }

        [Fact]
        public void ControlPlaneProject_KeepsDeterministicAssetBuildTargets()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj");

            Assert.Contains("Target Name=\"RestoreNodeModules\"", project);
            Assert.Contains("Target Name=\"CopyFontAwesomeAssets\"", project);
            Assert.Contains("Target Name=\"TailwindBuild\"", project);
            Assert.Contains("npm ci", project);
            Assert.Contains("npm run tailwind:build", project);
            Assert.Contains("@fortawesome\\fontawesome-free\\css\\all.min.css", project);
        }

        [Fact]
        public void ControlPlanePageHeader_DefinesOperationalHeaderExtensionPoint()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/ControlPlanePageHeader.razor");

            Assert.Contains("public string? Eyebrow", component);
            Assert.Contains("public string Title", component);
            Assert.Contains("public string? Description", component);
            Assert.Contains("public RenderFragment? Actions", component);
            Assert.Contains("[EditorRequired]", component);
            Assert.Contains("<h1 class=\"text-2xl font-bold text-ink-900\">@Title</h1>", component);
            Assert.DoesNotContain("cp-card", component, StringComparison.Ordinal);
            Assert.DoesNotContain("<main", component, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Home.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Stores.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceOrders.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommercePaymentMethods.razor")]
        public void ControlPlaneHighTrafficPages_UseSharedPageHeader(string relativePath)
        {
            var pageMarkup = ReadRepositoryFile(relativePath);

            Assert.Contains("<PageTitle>", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<ControlPlanePageHeader", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("Title=", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<Actions>", pageMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("ControlPlane.Web -> CommerceNode", pageMarkup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SharedV2BrowserHelpers_KeepJsModuleImports()
        {
            var sessionStorage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BrowserStorage/BrowserSessionStorageService.cs");
            var cookieStorage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/CookieStorage/BrowserCookieStorageService.cs");
            var authSync = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Authentication/AuthenticationSessionSyncService.cs");

            Assert.Contains("./js/sessionStorage.js", sessionStorage);
            Assert.Contains("./js/cookieStorage.js", cookieStorage);
            Assert.Contains("./js/authSessionSync.js", authSync);
        }

        [Fact]
        public void ArchitectureDocs_RecordStorefrontAssetOwnershipRules()
        {
            var projectGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");
            var decisionRules = ReadRepositoryFile("docs/architecture/08-agent-decision-rules.md");

            Assert.Contains("Root Storefront CSS and scripts must stay explicit in `App.razor`.", projectGuide);
            Assert.Contains("Page-specific JavaScript should prefer `IJSRuntime` module imports.", projectGuide);
            Assert.Contains("Store configuration must not accept arbitrary public script or stylesheet injection.", projectGuide);
            Assert.Contains("Keep root CSS and script entries in `BlazorShop.Storefront.V2/App.razor` allowlisted by tests.", decisionRules);
            Assert.Contains("Keep `blazor.web.js` before `storefrontCommerce.js`", decisionRules);
            Assert.Contains("Do not add DB-configured or store-configured arbitrary public scripts/styles.", decisionRules);
            Assert.Contains("Dynamic Storefront pages, maintenance pages, current-store/config reads, checkout/auth pages, SEO documents, and error states must not receive immutable cache headers.", decisionRules);
        }

        private static IReadOnlyList<string> ExtractStylesheetHrefs(string markup)
        {
            return StylesheetRegex()
                .Matches(markup)
                .Select(match => match.Groups["href"].Value)
                .Pipe(AssertNoDuplicates)
                .ToArray();
        }

        private static IReadOnlyList<string> ExtractScriptSources(string markup)
        {
            return ScriptRegex()
                .Matches(markup)
                .Select(match => match.Groups["src"].Value)
                .Pipe(AssertNoDuplicates)
                .ToArray();
        }

        private static IEnumerable<string> AssertNoDuplicates(IEnumerable<string> values)
        {
            var items = values.ToArray();
            var duplicates = items
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.Empty(duplicates);
            return items;
        }

        private static void AssertRootDoesNotReferenceLegacyPresentationAssets(string markup)
        {
            Assert.DoesNotContain("BlazorShop.Presentation", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("../BlazorShop.Presentation", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/internal/", markup, StringComparison.OrdinalIgnoreCase);
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
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

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
        }

        [GeneratedRegex("<link\\s+[^>]*rel=\"stylesheet\"[^>]*href=\"(?<href>[^\"]+)\"", RegexOptions.IgnoreCase)]
        private static partial Regex StylesheetRegex();

        [GeneratedRegex("<script\\s+[^>]*src=\"(?<src>[^\"]+)\"", RegexOptions.IgnoreCase)]
        private static partial Regex ScriptRegex();
    }

    internal static class LayoutAssetFoundationEnumerableExtensions
    {
        public static TResult Pipe<TValue, TResult>(this TValue value, Func<TValue, TResult> next)
        {
            return next(value);
        }
    }
}
